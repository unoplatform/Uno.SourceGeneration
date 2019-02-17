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
		EnvironmentDefinition _currentDefinition;

		private string[] Generate(BuildEnvironment environment)
		{
			if (
				_currentDefinition == null ||
				true // || (_currentDefinition?.IsInvalid ?? false)
			)
			{
				UnloadDomain();

				CreateDomain(environment);
			}
			else
			{
				// Try pinging the remote appdomain, as it may throw a RemotingException exception with this error:
				// Error : Object '/b612ce25_b538_486d_882a_28a019c782ed/p6lmd0em_mrlqpchbk5gh2mk_10.rem' has been disconnected or does not exist at the server.
				try
				{
					if (_currentDefinition.Engine.Ping())
					{
						this.Log().Debug($"Reusing generation host ({environment.ProjectFile}, {string.Join(", ", environment.SourceGenerators)})");
					}
				}
				catch (System.Runtime.Remoting.RemotingException /*e*/)
				{
					this.Log().Debug($"Discarding disconnected generation host for ({environment.ProjectFile}, {string.Join(", ", environment.SourceGenerators)})");

					CreateDomain(environment);
				}
			}

			RemotableLogger2 remotableLogger = new RemotableLogger2(this.Log());

			return _currentDefinition.Engine.Generate(remotableLogger, environment);
		}

		private void UnloadDomain()
		{
			if (_currentDefinition != null)
			{
				try
				{
					this.Log().Debug($"Unloading generation host {_currentDefinition.Domain.FriendlyName}");
					AppDomain.Unload(_currentDefinition.Domain);
				}
				catch (Exception /*e*/)
				{
					this.Log().Debug($"Failed to unload generation host {_currentDefinition.Domain.FriendlyName}");
				}
			}
		}

		private void CreateDomain(BuildEnvironment environment)
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
			setup.LoaderOptimization = LoaderOptimization.MultiDomainHost;
			setup.PrivateBinPath = setup.ShadowCopyDirectories;
			setup.ConfigurationFile = Path.Combine(wrapperBasePath, typeof(RemoteSourceGeneratorEngine).Assembly.GetName().Name + ".exe.config");

			var domain = AppDomain.CreateDomain("Generators-" + Guid.NewGuid(), null, setup);

			var newEngine = domain.CreateInstanceFromAndUnwrap(
				typeof(RemoteSourceGeneratorEngine).Assembly.CodeBase,
				typeof(RemoteSourceGeneratorEngine).FullName
			) as RemoteSourceGeneratorEngine;

			var msbuildBasePath = Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath);

			newEngine.MSBuildBasePath = msbuildBasePath;
			newEngine.AdditionalAssemblies = environment.AdditionalAssemblies;

			newEngine.Initialize();

			_currentDefinition = new EnvironmentDefinition(environment, domain, newEngine);
		}

		private class EnvironmentDefinition
		{
			private readonly BuildEnvironment _environment;
			private readonly DateTime _hostOwnerFileTimeStamp;
			private readonly DateTime[] _analyzersTimeStamps;

			public AppDomain Domain { get; }
			public RemoteSourceGeneratorEngine Engine { get; }

			public EnvironmentDefinition(BuildEnvironment environment, AppDomain domain, RemoteSourceGeneratorEngine engine)
			{
				Domain = domain;
				Engine = engine;
				_environment = environment;
				_hostOwnerFileTimeStamp = File.GetLastWriteTime(environment.ProjectFile);
				_analyzersTimeStamps = environment.SourceGenerators.Select(e => File.GetLastWriteTime(e)).ToArray();
			}

			public bool IsInvalid =>
				File.GetLastWriteTime(_environment.ProjectFile) != _hostOwnerFileTimeStamp
				|| !_environment.SourceGenerators.Select(e => File.GetLastWriteTime(e)).SequenceEqual(_analyzersTimeStamps);
		}
	}
}
#endif
