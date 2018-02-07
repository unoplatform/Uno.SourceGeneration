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
using MSB = Microsoft.Build;
using MSBE = Microsoft.Build.Execution;
using Microsoft.Extensions.Logging;
using Uno.SourceGeneration.host.Helpers;
using Uno.SourceGeneratorTasks;

namespace Uno.SourceGeneration.Host
{
	public class SourceGeneratorHost
	{
		private readonly BuildEnvironment _environment;

		public SourceGeneratorHost(BuildEnvironment environment)
		{
			_environment = environment;
		}

		public string[] Generate()
		{
			var globalExecution = Stopwatch.StartNew();

			var details = ProjectLoader.LoadProjectDetails(_environment);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Got project details after {globalExecution.Elapsed}");
			}

			if (!details.Generators.Any())
			{
				this.Log().Info($"No generators were found.");
				return new string[0];
			}

			var compilationResult = GetCompilation().Result;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Got compilation after {globalExecution.Elapsed}");
			}

			// Build dependencies graph
			var generatorsByName = details.Generators.ToDictionary(g => g.generatorType.FullName);
			var generatorNames = generatorsByName.Keys;

			// Dependencies list, in the form (before, after)
			var beforeDependencies = details.Generators
				.Select(g => g.generatorType)
				.SelectMany(t => t.GetCustomAttributes<SourceGeneratorDependencyAttribute>()
					.Select(a => a.DependsOn)
					.Intersect(generatorNames, StringComparer.InvariantCultureIgnoreCase)
					.Select(dependency => ((string incoming, string outgoing)) (t.FullName, dependency)));

			var afterDependencies = details.Generators
				.Select(g => g.generatorType)
				.SelectMany(t => t.GetCustomAttributes<DependentSourceGeneratorAttribute>()
					.Select(a => a.DependentGenerator)
					.Intersect(generatorNames, StringComparer.InvariantCultureIgnoreCase)
					.Select(dependent => ((string incoming, string outgoing))(dependent, t.FullName)));

			var dependencies = beforeDependencies.Concat(afterDependencies)
				.Where(x => x.incoming != x.outgoing)
				.Distinct()
				.ToList();

			if (dependencies.Any())
			{
				this.Log().Info($"Generators Dependencies:\n\t{dependencies.Select(d => $"{d.incoming} -> {d.outgoing}").JoinBy("\n\t")}");
			}

			var groupedGenerators = generatorNames.GroupSort(dependencies);

			if (groupedGenerators == null)
			{
				this.Log().Error("There is a cyclic dependency in the generators. You need to fix it. You may need to set your build output to 'normal' to see dependencies list.");
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
							var context = new InternalSourceGeneratorContext(compilationResult.compilation, compilationResult.project);
							context.SetProjectInstance(details.ExecutedProject);

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

		private async Task<(Compilation compilation, Project project)> GetCompilation()
		{
			try
			{
				var globalProperties = new Dictionary<string, string> {
							{ "Configuration", _environment.Configuration },
							{ "BuildingInsideVisualStudio", "true" },
							{ "BuildingInsideUnoSourceGenerator", "true" },
							{ "DesignTimeBuild", "true" },
							{ "UseHostCompilerIfAvailable", "false" },
							{ "UseSharedCompilation", "false" },
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

				var ws = MSBuildWorkspace.Create(globalProperties);

				ws.LoadMetadataForReferencedProjects = true;

				ws.WorkspaceFailed +=
					(s, e) => Console.WriteLine(e.Diagnostic.ToString());

				var project = await ws.OpenProjectAsync(_environment.ProjectFile);

				var metadataLessProjects = ws
					.CurrentSolution
					.Projects
					.Where(p => !p.MetadataReferences.Any())
					.ToArray();

				if (metadataLessProjects.Any())
				{
					// In this case, this may mean that Rolsyn failed to execute some msbuild task that loads the
					// references in a UWA project (or NuGet 3.0+ with project.json, more specifically). For these
					// projects, references are materialized through a task using a output parameter that injects 
					// "References" nodes. If this task fails, no references are loaded, and simple type resolution
					// such "int?" may fail.

					// Additionally, it may happen that projects are loaded using the callee's Configuration/Platform, which
					// may not exist in all projects. This can happen if the project does not have a proper
					// fallback mecanism in place.

					throw new InvalidOperationException(
						$"The project(s) {metadataLessProjects.Select(p => p.Name).JoinBy(",")} did not provide any metadata reference. " +
						"This may be due to an invalid path, such as $(SolutionDir) being used in the csproj; try using relative paths instead." +
						"This may also be related to a missing default configuration directive. Refer to the Uno.SourceGenerator Readme.md file for more details."
					);
				}

				project = RemoveGeneratedDocuments(project);

				var compilation = await project
						.GetCompilationAsync();

				// For some reason, this is required to avoid having a 
				// unbound NRE later during the execution when calling this exact same method;
				SyntaxFactory.ParseStatement("");

				return (compilation, project);
			}
			catch (Exception e)
			{
				var reflectionException = e as ReflectionTypeLoadException;

				if (reflectionException != null)
				{
					var loaderMessages = reflectionException.LoaderExceptions.Select(ex => ex.ToString()).JoinBy("\n");

					throw new InvalidOperationException(e.ToString() + "\nLoader Exceptions: " + loaderMessages);
				}
				else
				{
					throw new InvalidOperationException(e.ToString());
				}
			}
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
