// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// https://github.com/dotnet/roslyn/blob/main/src/Workspaces/Core/MSBuild/MSBuild/ProjectFile/ProjectFileInfo.cs

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Uno.SourceGeneration.Engine.Workspace.Constants;
using Uno.SourceGeneration.Host.Helpers;

namespace Uno.SourceGeneration.Engine.Workspace
{
    /// <summary>
    /// Provides information about a project that has been loaded from disk and
    /// built with MSBuild. If the project is multi-targeting, this represents
    /// the information from a single target framework.
    /// </summary>
    internal sealed class ProjectFileInfo
    {
        public bool IsEmpty { get; }

        /// <summary>
        /// The language of this project.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// The path to the project file for this project.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        public string OutputFilePath { get; }

        /// <summary>
        /// The path to the reference assembly output file this project generates.
        /// </summary>
        public string OutputRefFilePath { get; }

#if false
        /// <summary>
        /// The default namespace of the project ("" if not defined, which means global namespace),
        /// or null if it is unknown or not applicable. 
        /// </summary>
        /// <remarks>
        /// Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
        /// by assigning the value of the project's root namespace to it. So various feature can choose to 
        /// use it for their own purpose.
        /// In the future, we might consider officially exposing "default namespace" for VB project 
        /// (e.g. through a "defaultnamespace" msbuild property)
        /// </remarks>
        public string DefaultNamespace { get; }
#endif

        /// <summary>
        /// The target framework of this project.
        /// This takes the form of the 'short name' form used by NuGet (e.g. net46, netcoreapp2.0, etc.)
        /// </summary>
        public string TargetFramework { get; }

        /// <summary>
        /// The command line args used to compile the project.
        /// </summary>
        public ImmutableArray<string> CommandLineArgs { get; }

        /// <summary>
        /// The source documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> Documents { get; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> AdditionalDocuments { get; }

        /// <summary>
        /// The analyzer config documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> AnalyzerConfigDocuments { get; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public ImmutableArray<ProjectFileReference> ProjectReferences { get; }

#if false
        /// <summary>
        /// The error message produced when a failure occurred attempting to get the info. 
        /// If a failure occurred some or all of the information may be inaccurate or incomplete.
        /// </summary>
        public DiagnosticLog Log { get; }
#endif

        public override string ToString()
            => string.IsNullOrWhiteSpace(TargetFramework)
                ? FilePath ?? string.Empty
                : $"{FilePath} ({TargetFramework})";

        private ProjectFileInfo(
            bool isEmpty,
            string language,
            string filePath,
            string outputFilePath,
            string outputRefFilePath,
            //string defaultNamespace,
            string targetFramework,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences
            //DiagnosticLog log
			)
        {
            Debug.Assert(filePath != null);

            this.IsEmpty = isEmpty;
            this.Language = language;
            this.FilePath = filePath;
            this.OutputFilePath = outputFilePath;
            this.OutputRefFilePath = outputRefFilePath;
            //this.DefaultNamespace = defaultNamespace;
            this.TargetFramework = targetFramework;
            this.CommandLineArgs = commandLineArgs;
            this.Documents = documents;
            this.AdditionalDocuments = additionalDocuments;
            this.AnalyzerConfigDocuments = analyzerConfigDocuments;
            this.ProjectReferences = projectReferences;
            //this.Log = log;
        }

        public static ProjectFileInfo Create(
            string language,
            string filePath,
            string outputFilePath,
            string outputRefFilePath,
            //string defaultNamespace,
            string targetFramework,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences
            //DiagnosticLog log)
			)
            => new(
                isEmpty: false,
                language,
                filePath,
                outputFilePath,
                outputRefFilePath,
                //defaultNamespace,
                targetFramework,
                commandLineArgs,
                documents,
                additionalDocuments,
                analyzerConfigDocuments,
                projectReferences
                //log
				);

        public static ProjectFileInfo CreateEmpty(string language, string filePath) // , DiagnosticLog log
            => new(
                isEmpty: true,
                language,
                filePath,
                outputFilePath: null,
                outputRefFilePath: null,
                //defaultNamespace: null,
                targetFramework: null,
                commandLineArgs: ImmutableArray<string>.Empty,
                documents: ImmutableArray<DocumentFileInfo>.Empty,
                additionalDocuments: ImmutableArray<DocumentFileInfo>.Empty,
                analyzerConfigDocuments: ImmutableArray<DocumentFileInfo>.Empty,
                projectReferences: ImmutableArray<ProjectFileReference>.Empty
                //log
				);

		public static ProjectFileInfo FromMSBuildProjectInstance(string language, Microsoft.Build.Evaluation.Project loadedProject, ProjectInstance project)
			=> new Builder(loadedProject, project).Build(language);

		private class Builder
		{
			private IDictionary<string, ProjectItem> _documents;
			private readonly Microsoft.Build.Evaluation.Project _loadedProject;
			private readonly ProjectInstance _project;

			public Builder(Microsoft.Build.Evaluation.Project loadedProject, ProjectInstance project)
			{
				_loadedProject = loadedProject;
				_project = project;
			}

			public ProjectFileInfo Build(string language)
			{
				var commandLineArgs = GetCommandLineArgs(_project);

				var outputFilePath = _project.ReadPropertyString(PropertyNames.TargetPath);
				if (!string.IsNullOrWhiteSpace(outputFilePath))
				{
					outputFilePath = GetAbsolutePathRelativeToProject(outputFilePath);
				}

				var outputRefFilePath = _project.ReadPropertyString(PropertyNames.TargetRefPath);
				if (!string.IsNullOrWhiteSpace(outputRefFilePath))
				{
					outputRefFilePath = GetAbsolutePathRelativeToProject(outputRefFilePath);
				}

				var targetFramework = _project.ReadPropertyString(PropertyNames.TargetFramework);
				if (string.IsNullOrWhiteSpace(targetFramework))
				{
					targetFramework = null;
				}

				var docs = _project.GetDocuments()
					.Where(IsNotTemporaryGeneratedFile)
					.Select(d => MakeDocumentFileInfo(_project, d))
					.ToImmutableArray();

				var additionalDocs = _project.GetAdditionalFiles()
					.Select(MakeNonSourceFileDocumentFileInfo)
					.ToImmutableArray();

				var analyzerConfigDocs = _project.GetEditorConfigFiles()
					.Select(MakeNonSourceFileDocumentFileInfo)
					.ToImmutableArray();


				return ProjectFileInfo.Create(
					language,
					_loadedProject.FullPath,
					outputFilePath,
					outputRefFilePath,
					targetFramework,
					commandLineArgs,
					docs,
					additionalDocs,
					analyzerConfigDocs,
					_project.GetProjectReferences().ToImmutableArray()
				);
			}

			private DocumentFileInfo MakeNonSourceFileDocumentFileInfo(ITaskItem documentItem)
			{
				var filePath = GetDocumentFilePath(documentItem);
				var logicalPath = GetDocumentLogicalPath(documentItem, _project.Directory);
				var isLinked = IsDocumentLinked(documentItem);
				var isGenerated = IsDocumentGenerated(documentItem);
				return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated, SourceCodeKind.Regular);
			}

			private DocumentFileInfo MakeDocumentFileInfo(ProjectInstance project, ITaskItem documentItem)
			{
				var filePath = GetDocumentFilePath(documentItem);
				var logicalPath = GetDocumentLogicalPath(documentItem, project.Directory);
				var isLinked = IsDocumentLinked(documentItem);
				var isGenerated = IsDocumentGenerated(documentItem);
				var sourceCodeKind = GetSourceCodeKind(filePath);

				return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated, sourceCodeKind);
			}

			private SourceCodeKind GetSourceCodeKind(string documentFileName)
				=> SourceCodeKind.Regular;

			private bool IsDocumentGenerated(ITaskItem documentItem)
			{
				if (_documents == null)
				{
					_documents = new Dictionary<string, ProjectItem>();
					foreach (var item in _loadedProject.GetItems(ItemNames.Compile))
					{
						_documents[GetAbsolutePathRelativeToProject(item.EvaluatedInclude)] = item;
					}
				}

				return !_documents.ContainsKey(GetAbsolutePathRelativeToProject(documentItem.ItemSpec));
			}

			private bool IsDocumentLinked(ITaskItem documentItem)
				=> !string.IsNullOrEmpty(documentItem.GetMetadata(MetadataNames.Link));

			private string GetDocumentLogicalPath(ITaskItem documentItem, string projectDirectory)
			{
				var link = documentItem.GetMetadata(MetadataNames.Link);
				if (!string.IsNullOrEmpty(link))
				{
					// if a specific link is specified in the project file then use it to form the logical path.
					return link;
				}
				else
				{
					var result = documentItem.ItemSpec;
					if (Path.IsPathRooted(result))
					{
						// If we have an absolute path, there are two possibilities:
						result = Path.GetFullPath(result);

						// If the document is within the current project directory (or subdirectory), then the logical path is the relative path 
						// from the project's directory.
						if (result.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
						{
							result = result.Substring(projectDirectory.Length);
						}
						else
						{
							// if the document lies outside the project's directory (or subdirectory) then place it logically at the root of the project.
							// if more than one document ends up with the same logical name then so be it (the workspace will survive.)
							return Path.GetFileName(result);
						}
					}

					return result;
				}
			}

			private string GetDocumentFilePath(ITaskItem documentItem)
				=> GetAbsolutePathRelativeToProject(documentItem.ItemSpec);

			private bool IsNotTemporaryGeneratedFile(ITaskItem item)
				=> !Path.GetFileName(item.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal);

			/// <summary>
			/// Resolves the given path that is possibly relative to the project directory.
			/// </summary>
			/// <remarks>
			/// The resulting path is absolute but might not be normalized.
			/// </remarks>
			private string GetAbsolutePathRelativeToProject(string path)
			{
				// TODO (tomat): should we report an error when drive-relative path (e.g. "C:goo.cs") is encountered?
				var absolutePath = FileUtilities.ResolveRelativePath(path, _project.Directory) ?? path;
				return Path.GetFullPath(absolutePath);
			}

			private ImmutableArray<string> GetCommandLineArgs(ProjectInstance project)
			{
				var commandLineArgs = GetCompilerCommandLineArgs(project)
					.Select(item => item.ItemSpec)
					.ToImmutableArray();

				if (commandLineArgs.Length == 0)
				{
					// We didn't get any command-line args, which likely means that the build
					// was not successful. In that case, try to read the command-line args from
					// the ProjectInstance that we have. This is a best effort to provide something
					// meaningful for the user, though it will likely be incomplete.
					commandLineArgs = ReadCommandLineArgs(project);
				}

				return commandLineArgs;
			}

			private IEnumerable<ITaskItem> GetCompilerCommandLineArgs(ProjectInstance executedProject)
				=> executedProject.GetItems(ItemNames.CscCommandLineArgs);

			private ImmutableArray<string> ReadCommandLineArgs(ProjectInstance project)
				=> CSharpCommandLineArgumentReader.Read(project);
		}
    }
}
