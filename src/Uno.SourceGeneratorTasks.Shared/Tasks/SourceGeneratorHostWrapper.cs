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
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host;
using Uno.SourceGeneratorTasks.Helpers;
using Uno.SourceGeneratorTasks.Logger;
using System.Diagnostics;

namespace Uno.SourceGeneratorTasks
{
    public class SourceGeneratorHostWrapper : MarshalByRefObject
	{
        private readonly RemoteLoggerProvider _remoteLoggerProvider = new RemoteLoggerProvider();
		private bool _additionalAssembliesLoaded;

		public SourceGeneratorHostWrapper()
        {
            LogExtensionPoint.AmbientLoggerFactory.AddProvider(_remoteLoggerProvider);
            LogExtensionPoint.AmbientLoggerFactory.AddDebug();

            ApplyVS4MacWorkarounds();

            RegisterAssmblyLoader();

            AppDomain.CurrentDomain.DomainUnload += (s, e) => this.Log().Debug($"Unloading domain ({AppDomain.CurrentDomain.FriendlyName}");
        }

		public string MSBuildBasePath { get; set; }

		public string[] AdditionalAssemblies { get; set; }

		public override object InitializeLifetimeService()
		{
			// Keep this object alive infinitely, it will be deleted along with the 
			// host msbuild.exe process.
			return null;
		}

		internal string[] Generate(RemotableLogger2 logger, BuildEnvironment environment)
        {
            _remoteLoggerProvider.TaskLog = logger;

            return new SourceGeneratorHost(environment).Generate();
		}

		private void RegisterAssmblyLoader()
		{
			// Force assembly loader to consider siblings, when running in a separate appdomain.

			ResolveEventHandler localResolve = (s, e) =>
			{
                if(e.Name == "Mono.Runtime")
                {
                    // Roslyn 2.0 and later checks for the presence of the Mono runtime
                    // through this check.
                    return null;
                }

				var assembly = new AssemblyName(e.Name);
				var basePath = Path.GetDirectoryName(new Uri(this.GetType().Assembly.CodeBase).LocalPath);

				this.Log().Debug($"Searching for [{assembly}] from [{basePath}]");

				// Ignore resource assemblies for now, we'll have to adjust this
				// when adding globalization.
				if (assembly.Name.EndsWith(".resources"))
				{
					return null;
				}

				TryLoadAdditionalAssemblies();

				// Lookup for the highest version matching assembly in the current app domain.
				// There may be an existing one that already matches, even though the 
				// fusion loader did not find an exact match.
				var loadedAsm = (
									from asm in AppDomain.CurrentDomain.GetAssemblies()
									where asm.GetName().Name == assembly.Name
									orderby asm.GetName().Version descending
									select asm
								).ToArray();

				if(loadedAsm.Length > 1)
				{
					var duplicates = loadedAsm
						.Skip(1)
						.Where(a => a.GetName().Version == loadedAsm[0].GetName().Version)
						.ToArray();

					if (duplicates.Length != 0)
					{
						this.Log().Warn($"Selecting first occurrence of assembly [{e.Name}] which can be found at [{duplicates.Select(d => d.CodeBase).JoinBy("; ")}]");
					}

					return loadedAsm[0];
				}
				else if(loadedAsm.Length == 1)
				{
					return loadedAsm[0];
				}

				Assembly LoadAssembly(string filePath)
				{
					if (File.Exists(filePath))
					{
						try
						{
							var output = Assembly.LoadFrom(filePath);

							this.Log().Debug($"Loaded [{output.GetName()}] from [{output.CodeBase}]");

							return output;
						}
						catch (Exception ex)
						{
							this.Log().Debug($"Failed to load [{assembly}] from [{filePath}]", ex);
							return null;
						}
					}
					else
					{
						return null;
					}
				}

				var paths = new[] {
					Path.Combine(basePath, assembly.Name + ".dll"),
					Path.Combine(MSBuildBasePath, assembly.Name + ".dll"),
				};

				return paths
					.Select(LoadAssembly)
					.Where(p => p != null)
					.FirstOrDefault();
			};

			AppDomain.CurrentDomain.AssemblyResolve += localResolve;
            AppDomain.CurrentDomain.TypeResolve += localResolve;
		}

		private void TryLoadAdditionalAssemblies()
		{
			if(!_additionalAssembliesLoaded)
			{
				_additionalAssembliesLoaded = true;

				foreach (var assemblyPath in AdditionalAssemblies ?? new string[0])
				{
					try
					{
						var assembly = Assembly.LoadFrom(assemblyPath);
						this.Log().Debug($"Preloaded additional assembly [{assembly.FullName}] from [{assemblyPath}]");
					}
					catch (Exception e)
					{
						this.Log().Debug($"Failed to load additional assembly from [{assemblyPath}]", e);
					}
				}
			}
		}

		private static void ApplyVS4MacWorkarounds()
		{
			if (IsMono)
			{
				// This is a workaround for the loading of the VS4Mac cached version 
				// of this assembly when running the generation task from inside VS4Mac.
				// This will load the assembly located in the source generator binary folder.
				// This is not required for the CLI, though.
				Assembly.Load("System.Reflection.Metadata");
			}
		}

		/// <summary>
		/// Allows for the remote client to determine if the server is available.
		/// </summary>
		/// <returns></returns>
		internal bool Ping() => true;

		private static bool IsMono => Type.GetType("Mono.Runtime") != null;
	}
}
