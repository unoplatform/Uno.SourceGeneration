#if NETFRAMEWORK
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Uno.SourceGeneration.Helpers;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks;
using Uno.SourceGeneratorTasks.Helpers;
using Uno.SourceGeneratorTasks.Logger;

namespace Uno.SourceGeneration.Host.Server
{
	internal abstract partial class GenerationServerHost : IGenerationServerHost
	{
		RemoteSourceGeneratorEngine _remoteEngine;

		private string[] Generate(BuildEnvironment environment)
		{
			if (_remoteEngine == null)
			{
				_remoteEngine = CreateDomain(environment);
			}
			else
			{
				// Try pinging the remote appdomain, as it may throw a RemotingException exception with this error:
				// Error : Object '/b612ce25_b538_486d_882a_28a019c782ed/p6lmd0em_mrlqpchbk5gh2mk_10.rem' has been disconnected or does not exist at the server.
				try
				{
					if (_remoteEngine.Ping())
					{
						this.Log().Debug($"Reusing generation host ({environment.ProjectFile}, {string.Join(", ", environment.SourceGenerators)})");
					}
				}
				catch (System.Runtime.Remoting.RemotingException /*e*/)
				{
					this.Log().Debug($"Discarding disconnected generation host for ({environment.ProjectFile}, {string.Join(", ", environment.SourceGenerators)})");

					_remoteEngine = CreateDomain(environment);
				}
			}

			RemotableLogger2 remotableLogger = new RemotableLogger2(this.Log());

			return _remoteEngine.Generate(remotableLogger, environment);
		}

		private RemoteSourceGeneratorEngine CreateDomain(BuildEnvironment environment)
		{
			var generatorLocations = environment.SourceGenerators.Select(Path.GetFullPath).Select(Path.GetDirectoryName).Distinct();
			var wrapperBasePath = Path.GetDirectoryName(new Uri(typeof(RemoteSourceGeneratorEngine).Assembly.CodeBase).LocalPath);

			// We can create an app domain per OwnerFile and all Analyzers files
			// so that if those change, we can spin off another one, and still avoid
			// locking these assemblies.
			//
			// If the domain exists, keep it and continue generating content with it.

			var setup = new AppDomainSetup();
			setup.ApplicationBase = wrapperBasePath;
			setup.ShadowCopyFiles = "true";
			setup.ShadowCopyDirectories = string.Join(";", generatorLocations) + ";" + wrapperBasePath;
			setup.PrivateBinPath = setup.ShadowCopyDirectories;
			setup.ConfigurationFile = Path.Combine(wrapperBasePath, typeof(RemoteSourceGeneratorEngine).Assembly.GetName().Name + ".exe.config");

			// Loader optimization must not use MultiDomainHost, otherwise MSBuild assemblies may
			// be shared incorrectly when multiple versions are loaded in different domains.
			// The loader must specify SingleDomain, otherwise in contexts where devenv.exe is the
			// current process, the default optimization is "MultiDomain" and assemblies are 
			// incorrectly reused.
			setup.LoaderOptimization = LoaderOptimization.SingleDomain;

			var domain = AppDomain.CreateDomain("Generators-" + Guid.NewGuid(), null, setup);

			var newHost = domain.CreateInstanceFromAndUnwrap(
				typeof(RemoteSourceGeneratorEngine).Assembly.CodeBase,
				typeof(RemoteSourceGeneratorEngine).FullName
			) as RemoteSourceGeneratorEngine;

			var msbuildBasePath = Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath);

			newHost.MSBuildBasePath = msbuildBasePath;
			newHost.AdditionalAssemblies = environment.AdditionalAssemblies;

			newHost.Initialize();

			return newHost;
		}
	}
}
#endif
