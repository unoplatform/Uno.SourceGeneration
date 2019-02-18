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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Uno.SourceGeneration;
using Uno.SourceGeneratorTasks.Helpers;
using Microsoft.Extensions.Logging;
using Uno.SourceGeneration.host.Helpers;
using Uno.SourceGeneratorTasks;
using Uno.SourceGeneration.Engine.Workspace;
using System.Configuration;

namespace Uno.SourceGeneration.Host
{
	public partial class SourceGeneratorEngine
	{
		private readonly BuildEnvironment _environment;
		private readonly CachingMetadataReferenceResolver _metadataResolver;

		public SourceGeneratorEngine(BuildEnvironment environment, Func<string, MetadataReferenceProperties, MetadataReference> assemblyReferenceProvider)
		{
			_environment = environment;

			assemblyReferenceProvider = assemblyReferenceProvider ?? MetadataReferenceCache.Default.GetReference;

			_metadataResolver = new CachingMetadataReferenceResolver(
				(path, properties) => (PortableExecutableReference)assemblyReferenceProvider(path, properties)
			);

			PreInit();
		}

		private void PreInit()
		{
			// Used by MSB.ProjectCollection
			ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			// Parallelize the initialization of the MefServices graph (Used by AdHocWorkspace)
			Microsoft.CodeAnalysis.Host.Mef.MefHostServices.DefaultHost.ToString();

			// Pre-load known references
			if (_environment.ReferencePath != null)
			{
				foreach (var reference in _environment.ReferencePath)
				{
					_metadataResolver.ResolveReference(Path.GetFileName(reference), Path.GetDirectoryName(reference), new MetadataReferenceProperties());
				}
			}
		}

		public string[] Generate()
		{
			var globalExecution = Stopwatch.StartNew();

			using (var details = ProjectLoader.LoadProjectDetails(_environment, BuildGlobalMSBuildProperties()))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Got project details after {globalExecution.Elapsed}");
				}

				if (!details.Generators.Any())
				{
					this.Log().Info($"No generators were found.");
					return new string[0];
				}

				var compilationResult = GetCompilation(details).Result;

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Got compilation after {globalExecution.Elapsed}");
				}

				// Build dependencies graph
				var generatorsByName = details.Generators.ToDictionary(g => g.generatorType.FullName);
				var generatorNames = generatorsByName.Keys;

				// Dependencies list, in the form (before, after)
				var afterGenerators = details.Generators
					.Select(g => g.generatorType)
					.SelectMany(t => t.GetCustomAttributes<GenerateAfterAttribute>()
						.Select(a => a.GeneratorToExecuteBefore)
						.Intersect(generatorNames, StringComparer.InvariantCultureIgnoreCase)
						.Select(dependency => ((string incoming, string outgoing)) (t.FullName, dependency)));

				var beforeGenerators = details.Generators
					.Select(g => g.generatorType)
					.SelectMany(t => t.GetCustomAttributes<GenerateBeforeAttribute>()
						.Select(a => a.GeneratorToExecuteAfter)
						.Intersect(generatorNames, StringComparer.InvariantCultureIgnoreCase)
						.Select(dependent => ((string incoming, string outgoing))(dependent, t.FullName)));

				var dependencies = afterGenerators.Concat(beforeGenerators)
					.Where(x => x.incoming != x.outgoing)
					.Distinct()
					.ToList();

				if (dependencies.Any())
				{
					this.Log().Info($"Generators Ordering restrictions:\n\t{dependencies.Select(d => $"{d.incoming} -> {d.outgoing}").JoinBy("\n\t")}");
				}

				var groupedGenerators = generatorNames.GroupSort(dependencies);

				if (groupedGenerators == null)
				{
					this.Log().Error("There is a cyclic ordering in the generators. You need to fix it. You may need to set your build output to 'normal' to see dependencies list.");
					return new string[0];
				}

				if (dependencies.Any())
				{
					this.Log().Info($"**Generators Execution Plan**\n\tConcurrently: {groupedGenerators.Select(grp=>grp.JoinBy(", ")).JoinBy("\n\tFollowed by: ")}");
				}

				// Run
				var output = new List<string>();
				(string filePath, string content)[] generatedFilesAndContent = null;

				//Debugger.Launch();

				foreach (var group in groupedGenerators)
				{
					if (generatedFilesAndContent != null)
					{
						// We must recompile with the generated files of the previous group
						compilationResult = AddToCompilation(compilationResult, generatedFilesAndContent).Result;
					}

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"Running concurrently the following generators: {group.JoinBy(", ")}");
					}

					var generators = details.Generators.Where(g => group.Contains(g.generatorType.FullName));

					var generatorResults = generators
						.AsParallel()
						.Select(generatorDef =>
						{
							var generator = generatorDef.builder();

							try
							{
								var generatorLogger = new GeneratorLogger(generator.Log());

								var context = new InternalSourceGeneratorContext(compilationResult.compilation, compilationResult.project);
								context.SetProjectInstance(details.ExecutedProject);
								context.SetLogger(generatorLogger);

								generatorLogger.Debug($"{2}");

								var w = Stopwatch.StartNew();
								generator.Execute(context);

								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().Debug($"Ran {w.Elapsed} for [{generator.GetType()}]");
								}

								return new
								{
									Generator = generator,
									Context = context
								};
							}
							catch (Exception e)
							{
								// Wrap the exception into a string to avoid serialization issue when 
								// parts of the stack are coming from an assembly the msbuild task is 
								// not able to load properly.

								throw new InvalidOperationException($"Generation failed for {generator.GetType()}. {e}");
							}
						})
						.ToArray();

					generatedFilesAndContent = (from result in generatorResults
												from tree in result.Context.Trees
												select (
													Path.Combine(_environment.OutputPath ?? details.IntermediatePath, BuildTreeFileName(result.Generator, tree.Key)),
													tree.Value)
						)
						.ToArray();

					foreach (var file in generatedFilesAndContent)
					{
						Directory.CreateDirectory(Path.GetDirectoryName(file.filePath));

						if (File.Exists(file.filePath))
						{
							if (File.ReadAllText(file.filePath).SequenceEqual(file.content))
							{
								if (this.Log().IsEnabled(LogLevel.Information))
								{
									this.Log().Info($"Skipping generated file with same content: {file.filePath}");
								}

								continue;
							}
							else
							{
								if (this.Log().IsEnabled(LogLevel.Information))
								{
									this.Log().Info($"Overwriting generated file with different content: {file.filePath}");
								}
							}
						}

						File.WriteAllText(file.filePath, file.content);
					}

					output.AddRange(generatedFilesAndContent.Select(f => f.filePath));
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Code generation ran for {globalExecution.Elapsed}");
				}

				return output.ToArray();
			}
		}

		private string BuildTreeFileName(SourceGenerator generator, string key)
		{
			key = key
				.Replace(":", "_")
				.Replace(" ", "_")
				.Replace("\\", "_")
				.Replace("//", "_")
				.Replace("<", "_")
				.Replace(">", "_")
				.Replace(",", "_")
				.Replace(":", "_")
				.Replace(".", "_");

			return Path.Combine(generator.GetType().Name, key + $".g.cs");
		}

		private async Task<(Compilation compilation, Project project)> GetCompilation(ProjectDetails details)
		{
			var projectFileInfo = ProjectFileInfo.FromMSBuildProjectInstance(LanguageNames.CSharp, details.LoadedProject, details.ExecutedProject);

			var pi = ProjectInfoBuilder.CreateProjectInfo(projectFileInfo);

			var ws2 = new AdhocWorkspace();
			var solution = ws2.CurrentSolution.AddProject(pi);
			var project = solution.GetProject(pi.Id);

			if (_environment.ReferencePath != null)
			{
				foreach (var reference in _environment.ReferencePath)
				{
					var metadataRefs = _metadataResolver.ResolveReference(Path.GetFileName(reference), Path.GetDirectoryName(reference), new MetadataReferenceProperties());

					foreach (var metadataRef in metadataRefs)
					{
						project = project.AddMetadataReference(metadataRef);
					}
				}
			}

			project = RemoveGeneratedDocuments(project);

			var compilation = await project
					.GetCompilationAsync();

			// For some reason, this is required to avoid having a 
			// unbound NRE later during the execution when calling this exact same method;
			SyntaxFactory.ParseStatement("");

			return (compilation, project);
		}


		private Dictionary<string, string> BuildGlobalMSBuildProperties()
		{
			var globalProperties = new Dictionary<string, string> {
				// Default global properties defined in Microsoft.CodeAnalysis.MSBuild.Build.ProjectBuildManager
				// https://github.com/dotnet/roslyn/blob/b9fb1610c87cccc8ceb74a770dba261a58e39c4a/src/Workspaces/Core/MSBuild/MSBuild/Build/ProjectBuildManager.cs#L24

				{ "BuildProjectReferences", bool.FalseString },
				// this will tell msbuild to not build the dependent projects
				{ "DesignTimeBuild", bool.TrueString },
				// don't actually run the compiler
				{ "SkipCompilerExecution", bool.TrueString },

				{ "UseHostCompilerIfAvailable", bool.FalseString },
				{ "UseSharedCompilation", bool.FalseString },

				// Prevent the generator to recursively execute
				{ "BuildingInsideUnoSourceGenerator", bool.TrueString },
				{ "Configuration", _environment.Configuration },

				// Override the output path so custom compilation lists do not override the
				// main compilation caches, which can invalidate incremental compilation.
				{ "IntermediateOutputPath", Path.Combine(_environment.OutputPath, "obj") + Path.DirectorySeparatorChar },
				{ "VisualStudioVersion", _environment.VisualStudioVersion },

				// The Platform is intentionally not set here
				// as for now, Roslyn applies the properties to all 
				// loaded projects, directly or indirectly.
				// So for now, we rely on the fact that all projects
				// have a default platform directive, and that most projects
				// don't rely on the platform to adjust the generated code.
				// (e.g. _platform may be iPhoneSimulator, but all projects may not
				// support this target, and therefore will fail to load.
				//{ "Platform", _platform },
			};

#if !NETCOREAPP
			// This will force CoreCompile task to execute even if all inputs and outputs are up to date
			// 
			// Note that this is disabled for NETCOREAPP because of the absence of the "ResolveNonMSBuildProjectOutput"
			// task which is only executed when running under VisualStudio.
			// See for reference: https://github.com/Microsoft/msbuild/blob/a972ec96c3920705e4e8d03d7ac8b6c3328450bd/src/Tasks/Microsoft.Common.CurrentVersion.targets#L1548
			globalProperties.Add("BuildingInsideVisualStudio", bool.TrueString);
#endif

			if (_environment.TargetFramework.HasValue())
			{
				// Target framework is required for the MSBuild 15.0 Cross Compilation.
				// Loading a project without the target framework results in an empty project, which interatively
				// sets the TargetFramework property.
				globalProperties.Add("TargetFramework", _environment.TargetFramework);
			}

			// TargetFrameworkRootPath is used by VS4Mac to determine the
			// location of frameworks like Xamarin.iOS.
			if (_environment.TargetFrameworkRootPath.HasValue())
			{
				globalProperties["TargetFrameworkRootPath"] = _environment.TargetFrameworkRootPath;
			}

			return globalProperties;
		}

		private async Task<(Compilation compilation, Project project)> AddToCompilation(
			(Compilation compilation, Project project) previousCompilation,
			IEnumerable<(string name, string content)> files)
		{
			var (_, project) = previousCompilation;

			foreach (var (name, content) in files)
			{
				project = project.AddDocument(name, content, filePath: name).Project;
			}

			return (await project.GetCompilationAsync(), project);
		}

		private Project RemoveGeneratedDocuments(Project project)
		{
			// Remove all previously generated documents from the list.
			// The generator does not support generators dependencies, which makes 
			// generators sensitive to existing methods depending on the order of 
			// execution.

			var generatedDocs = project
								.Documents
								.Where(d => d.FilePath.StartsWith(_environment.OutputPath))
								.Select(d => d.Id)
								.ToArray();

			if (generatedDocs.Any())
			{
				foreach (var doc in generatedDocs)
				{
					project = project.RemoveDocument(doc);
				}
			}

			return project;
		}
	}
}
