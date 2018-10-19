// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host;
using Uno.SourceGeneratorTasks.Helpers;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Uno.SourceGeneration.Helpers;

namespace Uno.SourceGeneratorTasks
{
	public class SourceGenerationTask_v0 : Microsoft.Build.Utilities.Task
	{
		[Required]
		public string ProjectFile { get; set; }

		[Required]
		public string Platform { get; set; }

		[Required]
		public string Configuration { get; set; }

		public string TargetFramework { get; set; }

		public string VisualStudioVersion { get; set; }

		public string TargetFrameworkRootPath { get; set; }

		/// <summary>
		/// Determines the use of the external source generation host.
		/// Enabled by default when running under Mono and .NET Core.
		/// </summary>
		public string UseGenerationHost { get; set; }

		/// <summary>
		/// Capture the generation host standard output for debug purposes.
		/// (used when UseGenerationHost is set)
		/// </summary>
		public string CaptureGenerationHostOutput { get; set; }

		/// <summary>
		/// Provides a list of assemblies to be loaded in the SourceGenerator
		/// secondary app domains. This is a backward compatibility feature related
		/// to the use of external libraries in previous versions of the SourceGeneration task.
		/// </summary>
		public string[] AdditionalAssemblies { get; set; }

		[Required]
		public string[] SourceGenerators { get; set; }

		public string OutputPath { get; set; }

		public string BinLogOutputPath { get; set; }

		public bool BinLogEnabled { get; set; }

		[Output]
		public string[] GenereratedFiles { get; set; }

		private static ConcurrentDictionary<DomainEntry, HostCollection> _domains =
			new ConcurrentDictionary<DomainEntry, HostCollection>();

		private TaskLoggerProvider _taskLogger;

		public override bool Execute()
		{
			string lockFile = null;

			try
			{
				lockFile = Path.Combine(OutputPath, "unoGenerator.lock");

				if (File.Exists(lockFile))
				{
					// This may happen during the initial load of the project. At this point
					// there is no need to re-generate the files.
					return true;
				}

				if (NeedsGenerationHost)
				{
					GenerateWithHost();
				}
				else
				{
					GenerateWithAppDomains(lockFile);
				}

				return true;
			}
			catch (Exception e)
			{
				var aggregate = e as AggregateException;

				if (aggregate != null)
				{
					this.Log.LogError(string.Join(", ", aggregate.InnerExceptions.Select(ie => ie.Message)));
				}
				else
				{
					this.Log.LogError(e.Message);
				}

				this.Log.LogMessage(MessageImportance.Low, e.ToString());

				return false;
			}
			finally
			{
				if (File.Exists(lockFile))
				{
					File.Delete(lockFile);
				}
			}
		}

		private void GenerateWithHost()
		{
			var captureHostOutput = false;
			if (!bool.TryParse(this.CaptureGenerationHostOutput, out captureHostOutput))
			{
#if DEBUG
				captureHostOutput = true; // default to true in DEBUG
#else
				captureHostOutput = false; // default to false in RELEASE
#endif
			}

			var hostPath = GetHostPath();

			var responseFile = Path.GetTempFileName();
			var outputFile = Path.GetTempFileName();
			var binlogFile = Path.GetTempFileName() + ".binlog";

			try
			{
				using (var responseStream = File.OpenWrite(responseFile))
				{
					var serializer = new DataContractSerializer(typeof(BuildEnvironment));
					serializer.WriteObject(responseStream, CreateBuildEnvironment());
				}

				ProcessStartInfo buildInfo()
				{
					if (RuntimeHelpers.IsNetCore)
					{
						var hostBinPath = Path.Combine(hostPath, "Uno.SourceGeneration.Host.dll");
						string arguments = $"\"{hostBinPath}\" \"{responseFile}\" \"{outputFile}\" \"{binlogFile}\"";
						var pi = new ProcessStartInfo("dotnet", arguments);
						pi.UseShellExecute = false;

						return pi;
					}
					else
					{
						var hostBinPath = Path.Combine(hostPath, "Uno.SourceGeneration.Host.exe");
						string arguments = $"\"{responseFile}\" \"{outputFile}\" \"{binlogFile}\"";
						var pi = new ProcessStartInfo(hostBinPath, arguments);
						pi.UseShellExecute = false;

						return pi;
					}
				}

				using (var process = new Process())
				{
					var startInfo = buildInfo();

					if (captureHostOutput)
					{
						startInfo.Arguments += " -console";
						startInfo.RedirectStandardOutput = true;
						startInfo.RedirectStandardError = true;

						process.StartInfo = startInfo;
						process.Start();

						var output = process.StandardOutput.ReadToEnd();
						var error = process.StandardError.ReadToEnd();
						process.WaitForExit();

						this.Log().Info(
							$"Executing {startInfo.FileName} {startInfo.Arguments}:\n" +
							$"result: {process.ExitCode}\n" +
							$"\n---begin host output---\n{output}\n" +
							$"---begin host ERROR output---\n{error}\n" +
							"---end host output---\n");
					}
					else
					{
						process.StartInfo = startInfo;
						process.Start();
						process.WaitForExit();
					}

					BinaryLoggerReplayHelper.Replay(BuildEngine, binlogFile);

					if (process.ExitCode == 0)
					{
						GenereratedFiles = File.ReadAllText(outputFile).Split(';');
					}
					else
					{
						throw new InvalidOperationException($"Generation failed");
					}
				}
			}
			finally
			{
				File.Delete(responseFile);
				File.Delete(outputFile);
				File.Delete(binlogFile);
			}
		}


		public bool NeedsGenerationHost
			=> (bool.TryParse(UseGenerationHost, out var result) && result)
			|| RuntimeHelpers.IsMono
			|| RuntimeHelpers.IsNetCore;

		private string GetHostPath()
		{
			var currentPath = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath);
			var hostPlatform = RuntimeHelpers.IsNetCore ? "netcoreapp2.1" : "net462";
			var installedPath = Path.Combine(currentPath, "..", "host", hostPlatform);
#if DEBUG
			var configuration = "Debug";
#else
			var configuration = "Release";
#endif

			var devPath = Path.Combine(currentPath, "..", "..", "..", "Uno.SourceGeneration.Host", "bin", configuration, hostPlatform);

			if (Directory.Exists(devPath))
			{
				return devPath;
			}
			else if (Directory.Exists(installedPath))
			{
				return installedPath;
			}
			else
			{
				throw new InvalidOperationException($"Unable to find Uno.SourceGeneration.Host.dll (in {devPath} or {installedPath})");
			}
			
		}

		private void GenerateWithAppDomains(string lockFile)
		{
			Directory.CreateDirectory(OutputPath);
			File.WriteAllText(lockFile, "");

			Log.LogMessage("Running Source Generation Task");

			_taskLogger = new TaskLoggerProvider() { TaskLog = Log };
			LogExtensionPoint.AmbientLoggerFactory.AddProvider(_taskLogger);

			var collection = GetCollection();

			GenerateForCollection(collection);
		}

		private HostCollection GetCollection()
		{
			var entry = new DomainEntry(this.BuildEngine4.ProjectFileOfTaskNode, Platform, SourceGenerators.Select(Path.GetFullPath).ToArray());

			HostCollection collection = GetHost(entry);

			if (collection.IsInvalid)
			{
				Log.LogMessage("Discarding source generation host, generators files have been modified");

				_domains.TryRemove(entry, out collection);

				UnloadHosts(collection);

				collection = GetHost(entry);
			}

			return collection;
		}

		private void UnloadHosts(HostCollection collection)
		{
			foreach (var host in collection.Hosts)
			{
				try
				{
					Log.LogMessage($"Unloading generation host {host.Domain.FriendlyName}");
					AppDomain.Unload(host.Domain);
				}
				catch (Exception /*e*/)
				{
					Log.LogWarning($"Failed to unload generation host {host.Domain.FriendlyName}");
				}
			}
		}

		private void GenerateForCollection(HostCollection collection)
		{
			(SourceGeneratorHostWrapper Wrapper, AppDomain Domain) hostEntry = (null, null);

			try
			{
				if (!collection.Hosts.TryTake(out hostEntry))
				{
					hostEntry = CreateDomain(collection);
				}
				else
				{
					// Try pinging the remote appdomain, as it may throw a RemotingException exception with this error:
					// Error : Object '/b612ce25_b538_486d_882a_28a019c782ed/p6lmd0em_mrlqpchbk5gh2mk_10.rem' has been disconnected or does not exist at the server.
					try
					{
						if (hostEntry.Wrapper.Ping())
						{
							Log.LogMessage($"Reusing generation host ({collection.Entry.OwnerFile}, {string.Join(", ", collection.Entry.Analyzers)})");
						}
					}
					catch (System.Runtime.Remoting.RemotingException /*e*/)
					{
						Log.LogMessage($"Discarding disconnected generation host for ({collection.Entry.OwnerFile}, {string.Join(", ", collection.Entry.Analyzers)})");

						hostEntry = CreateDomain(collection);
					}
				}

				Logger.RemotableLogger2 remotableLogger = new Logger.RemotableLogger2(_taskLogger.CreateLogger("Logger.RemotableLogger"));

				var environment = CreateBuildEnvironment();

				GenereratedFiles = hostEntry.Wrapper.Generate(remotableLogger, environment);
			}
			finally
			{
				if (collection != null)
				{
					collection.Hosts.Add(hostEntry);
				}
			}
		}

		private BuildEnvironment CreateBuildEnvironment()
			=> new BuildEnvironment
			{
				Configuration = Configuration,
				Platform = Platform,
				ProjectFile = ProjectFile,
				OutputPath = OutputPath,
				TargetFramework = TargetFramework,
				VisualStudioVersion = VisualStudioVersion,
				TargetFrameworkRootPath = TargetFrameworkRootPath,
				BinLogOutputPath = BinLogOutputPath,
				BinLogEnabled = BinLogEnabled,
				MSBuildBinPath = Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath),
				AdditionalAssemblies = AdditionalAssemblies
			};

		private (SourceGeneratorHostWrapper Wrapper, AppDomain Domain) CreateDomain(HostCollection collection)
		{
			(SourceGeneratorHostWrapper Wrapper, AppDomain Domain) hostEntry;
			Log.LogMessage($"Creating generation host ({collection.Entry.OwnerFile}, {string.Join(", ", collection.Entry.Analyzers)}, {Environment.OSVersion.Platform})");

			var generatorLocations = SourceGenerators.Select(Path.GetFullPath).Select(Path.GetDirectoryName).Distinct();
			var wrapperBasePath = Path.GetDirectoryName(new Uri(typeof(SourceGeneratorHostWrapper).Assembly.CodeBase).LocalPath);

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
			setup.ConfigurationFile = Path.Combine(wrapperBasePath, typeof(SourceGeneratorHostWrapper).Assembly.GetName().Name + ".dll.config");

			// Loader optimization must not use MultiDomainHost, otherwise MSBuild assemblies may
			// be shared incorrectly when multiple versions are loaded in different domains.
			// The loader must specify SingleDomain, otherwise in contexts where devenv.exe is the
			// current process, the default optimization is "MultiDomain" and assemblies are 
			// incorrectly reused.
			setup.LoaderOptimization = LoaderOptimization.SingleDomain;

			var domain = AppDomain.CreateDomain("Generators-" + Guid.NewGuid(), null, setup);

			Log.LogMessage($"[{Process.GetCurrentProcess().ProcessName}] Creating object {typeof(SourceGeneratorHostWrapper).Assembly.CodeBase} with {typeof(SourceGeneratorHostWrapper).FullName}. wrapperBasePath {wrapperBasePath} ");

			var newHost = domain.CreateInstanceFromAndUnwrap(
				typeof(SourceGeneratorHostWrapper).Assembly.CodeBase,
				typeof(SourceGeneratorHostWrapper).FullName
			) as SourceGeneratorHostWrapper;

			var msbuildBasePath = Path.GetDirectoryName(new Uri(typeof(Microsoft.Build.Logging.ConsoleLogger).Assembly.CodeBase).LocalPath);

			newHost.MSBuildBasePath = msbuildBasePath;
			newHost.AdditionalAssemblies = AdditionalAssemblies;

			newHost.Initialize();

			hostEntry = (newHost, domain);
			return hostEntry;
		}

		private static HostCollection GetHost(DomainEntry entry)
			=> _domains.TryGetValue(entry, out var hosts)
			? hosts
			: _domains[entry] = new HostCollection(entry);
	}
}
