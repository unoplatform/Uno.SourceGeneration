#if NETFRAMEWORK
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
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
		private static ConcurrentDictionary<EnvironmentPoolEntry, EnvironmentPool> _domains =
			new ConcurrentDictionary<EnvironmentPoolEntry, EnvironmentPool>();

		private string[] Generate(BuildEnvironment environment)
		{
			var collection = FindEnvironmentPool(environment);

			return GenerateForCollection(collection, environment);
		}

		private string[] GenerateForCollection(EnvironmentPool collection, BuildEnvironment environment)
		{
			(RemoteSourceGeneratorEngine Wrapper, AppDomain Domain) hostEntry = (null, null);

			try
			{
				if (!collection.Hosts.TryTake(out hostEntry))
				{
					hostEntry = CreateDomain(environment);
				}
				else
				{
					// Try pinging the remote appdomain, as it may throw a RemotingException exception with this error:
					// Error : Object '/b612ce25_b538_486d_882a_28a019c782ed/p6lmd0em_mrlqpchbk5gh2mk_10.rem' has been disconnected or does not exist at the server.
					try
					{
						if (hostEntry.Wrapper.Ping())
						{
							this.Log().Debug($"Reusing generation host ({collection.Entry.ProjectFile}, {string.Join(", ", collection.Entry.SourceGenerators)})");
						}
					}
					catch (System.Runtime.Remoting.RemotingException /*e*/)
					{
						this.Log().Debug($"Discarding disconnected generation host for ({collection.Entry.ProjectFile}, {string.Join(", ", collection.Entry.SourceGenerators)})");

						hostEntry = CreateDomain(environment);
					}
				}

				var remotableLogger = new RemotableLogger2(this.Log());

				return hostEntry.Wrapper.Generate(remotableLogger, environment);
			}
			finally
			{
				if (collection != null)
				{
					collection.Hosts.Add(hostEntry);
				}
			}
		}

		private EnvironmentPool FindEnvironmentPool(BuildEnvironment environment)
		{
			var entry = new EnvironmentPoolEntry(environment.ProjectFile, environment.Platform, environment.SourceGenerators.Select(Path.GetFullPath).ToArray());

			var collection = GetEnvironmentPool(entry);

			if (collection.IsInvalid)
			{
				this.Log().Debug("Discarding source generation host, generators files have been modified");

				_domains.TryRemove(entry, out collection);

				UnloadHosts(collection);

				collection = GetEnvironmentPool(entry);
			}

			return collection;
		}

		private void UnloadHosts(EnvironmentPool collection)
		{
			foreach (var host in collection.Hosts)
			{
				try
				{
					this.Log().Debug($"Unloading generation host {host.Domain.FriendlyName}");
					AppDomain.Unload(host.Domain);
				}
				catch (Exception /*e*/)
				{
					this.Log().Debug($"Failed to unload generation host {host.Domain.FriendlyName}");
				}
			}
		}

		private (RemoteSourceGeneratorEngine, AppDomain) CreateDomain(BuildEnvironment environment)
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

			// MultiDomainHost can be used, only because it's not redirecting non-GAC assemblies.
			setup.LoaderOptimization = LoaderOptimization.MultiDomainHost;

			var domain = AppDomain.CreateDomain("Generators-" + Guid.NewGuid(), null, setup);

			var generationEngine = domain.CreateInstanceFromAndUnwrap(
				typeof(RemoteSourceGeneratorEngine).Assembly.CodeBase,
				typeof(RemoteSourceGeneratorEngine).FullName
			) as RemoteSourceGeneratorEngine;

			var msbuildBasePath = Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath);

			generationEngine.MSBuildBasePath = msbuildBasePath;
			generationEngine.AdditionalAssemblies = environment.AdditionalAssemblies;

			generationEngine.Initialize();

			return (generationEngine, domain);
		}

		private static EnvironmentPool GetEnvironmentPool(EnvironmentPoolEntry entry)
			=> _domains.TryGetValue(entry, out var hosts)
				? hosts
				: _domains[entry] = new EnvironmentPool(entry);
	}
}
#endif
