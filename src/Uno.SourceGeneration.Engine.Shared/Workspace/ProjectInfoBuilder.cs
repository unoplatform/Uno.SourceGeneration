using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Uno.SourceGeneration.Engine.Workspace.Utilities;
using Uno.SourceGeneration.Host;
using Uno.SourceGeneration.Host.Helpers;

namespace Uno.SourceGeneration.Engine.Workspace
{
	internal class ProjectInfoBuilder
	{
		public static ProjectInfo CreateProjectInfo(ProjectFileInfo projectFileInfo)
		{
			var projectDirectory = Path.GetDirectoryName(projectFileInfo.FilePath);

			var projectId = ProjectId.CreateNewId(debugName: projectFileInfo.FilePath);
			var version = VersionStamp.Create(FileUtilities.GetFileTimeStamp(projectFileInfo.FilePath));
			var projectName = Path.GetFileNameWithoutExtension(projectFileInfo.FilePath);

			// parse command line arguments
			var commandLineParser = CSharpCommandLineParser.Default;

			var commandLineArgs = commandLineParser.Parse(
				args: projectFileInfo.CommandLineArgs,
				baseDirectory: projectDirectory,
				sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

			var assemblyName = commandLineArgs.CompilationName;
			if (string.IsNullOrWhiteSpace(assemblyName))
			{
				// if there isn't an assembly name, make one from the file path.
				// Note: This may not be necessary any longer if the commmand line args
				// always produce a valid compilation name.
				assemblyName = GetAssemblyNameFromProjectPath(projectFileInfo.FilePath);
			}

			// Ensure sure that doc-comments are parsed
			var parseOptions = commandLineArgs.ParseOptions;
			if (parseOptions.DocumentationMode == DocumentationMode.None)
			{
				parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Parse);
			}

			// add all the extra options that are really behavior overrides
			var metadataService = _metadataService;
			var compilationOptions = commandLineArgs.CompilationOptions
				.WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
				.WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory))
				// TODO: https://github.com/dotnet/roslyn/issues/4967
				.WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory)))
				.WithStrongNameProvider(new DesktopStrongNameProvider(commandLineArgs.KeyFileSearchPaths))
				.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

			var documents = CreateDocumentInfos(projectFileInfo.Documents, projectId, commandLineArgs.Encoding);
			var additionalDocuments = CreateDocumentInfos(projectFileInfo.AdditionalDocuments, projectId, commandLineArgs.Encoding);
			// CheckForDuplicateDocuments(documents, additionalDocuments, projectPath, projectId);

			var resolvedReferences = ResolveReferencesAsync(projectId, projectFileInfo, commandLineArgs);

			return ProjectInfo.Create(
				projectId,
				version,
				projectName,
				assemblyName,
				LanguageNames.CSharp,
				projectFileInfo.FilePath,
				outputFilePath: projectFileInfo.OutputFilePath,
				compilationOptions: compilationOptions,
				parseOptions: parseOptions,
				documents: documents,
				projectReferences: resolvedReferences.ProjectReferences.Distinct(),
				metadataReferences: resolvedReferences.MetadataReferences.Distinct(),
				analyzerReferences: Enumerable.Empty<AnalyzerReference>(),
				additionalDocuments: additionalDocuments,
				isSubmission: false,
				hostObjectType: null);
		}

		private readonly struct ResolvedReferences
		{
			public ImmutableArray<ProjectReference> ProjectReferences { get; }
			public ImmutableArray<MetadataReference> MetadataReferences { get; }

			public ResolvedReferences(ImmutableArray<ProjectReference> projectReferences, ImmutableArray<MetadataReference> metadataReferences)
			{
				ProjectReferences = projectReferences;
				MetadataReferences = metadataReferences;
			}
		}

		static MetadataService _metadataService = new MetadataService();

		private static ResolvedReferences ResolveReferencesAsync(ProjectId id, ProjectFileInfo projectFileInfo, CommandLineArguments commandLineArgs)
		{
			// First, gather all of the metadata references from the command-line arguments.
			var resolvedMetadataReferences = commandLineArgs.ResolveMetadataReferences(
				new WorkspaceMetadataFileReferenceResolver(
					metadataService: _metadataService,
					pathResolver: new RelativePathResolver(commandLineArgs.ReferencePaths, commandLineArgs.BaseDirectory)));

			var builder = new ResolvedReferencesBuilder(resolvedMetadataReferences);

			var projectDirectory = Path.GetDirectoryName(projectFileInfo.FilePath);

			return builder.ToResolvedReferences();
		}


		/// <summary>
		/// This type helps produces lists of metadata and project references. Initially, it contains a list of metadata references.
		/// As project references are added, the metadata references that match those project references are removed.
		/// </summary>
		private class ResolvedReferencesBuilder
		{
			/// <summary>
			/// The full list of <see cref="MetadataReference"/>s.
			/// </summary>
			private readonly ImmutableArray<MetadataReference> _metadataReferences;

			/// <summary>
			/// A map of every metadata reference file paths to a set of indices whether than file path
			/// exists in the list. It is expected that there may be multiple metadata references for the
			/// same file path in the case where multiple extern aliases are provided.
			/// </summary>
			private readonly ImmutableDictionary<string, HashSet<int>> _pathToIndicesMap;

			/// <summary>
			/// A set of indeces into <see cref="_metadataReferences"/> that are to be removed.
			/// </summary>
			private readonly HashSet<int> _indicesToRemove;

			private readonly ImmutableArray<ProjectReference>.Builder _projectReferences;

			public ResolvedReferencesBuilder(IEnumerable<MetadataReference> metadataReferences)
			{
				_metadataReferences = metadataReferences.ToImmutableArray();
				_pathToIndicesMap = CreatePathToIndexMap(_metadataReferences);
				_indicesToRemove = new HashSet<int>();
				_projectReferences = ImmutableArray.CreateBuilder<ProjectReference>();
			}

			private static ImmutableDictionary<string, HashSet<int>> CreatePathToIndexMap(ImmutableArray<MetadataReference> metadataReferences)
			{
				var builder = ImmutableDictionary.CreateBuilder<string, HashSet<int>>(PathUtilities.Comparer);

				for (var index = 0; index < metadataReferences.Length; index++)
				{
					var filePath = GetFilePath(metadataReferences[index]);
					if (filePath != null)
					{
						builder.MultiAdd(filePath, index);
					}
				}

				return builder.ToImmutable();
			}

			private static string GetFilePath(MetadataReference metadataReference)
			{
				switch (metadataReference)
				{
					case PortableExecutableReference portableExecutableReference:
						return portableExecutableReference.FilePath;
					case UnresolvedMetadataReference unresolvedMetadataReference:
						return unresolvedMetadataReference.Reference;
					default:
						return null;
				}
			}

			public void AddProjectReference(ProjectReference projectReference)
			{
				_projectReferences.Add(projectReference);
			}

			public void SwapMetadataReferenceForProjectReference(ProjectReference projectReference, params string[] possibleMetadataReferencePaths)
			{
				foreach (var path in possibleMetadataReferencePaths)
				{
					Remove(path);
				}

				AddProjectReference(projectReference);
			}

			/// <summary>
			/// Returns true if a metadata reference with the given file path is contained within this list.
			/// </summary>
			public bool Contains(string filePath)
				=> filePath != null
				&& _pathToIndicesMap.ContainsKey(filePath);

			/// <summary>
			/// Removes the metadata reference with the given file path from this list.
			/// </summary>
			public void Remove(string filePath)
			{
				if (filePath != null && _pathToIndicesMap.TryGetValue(filePath, out var indices))
				{
					foreach (var index in indices)
					{
						_indicesToRemove.Add(index);
					}
				}
			}

			public ProjectInfo SelectProjectInfoByOutput(IEnumerable<ProjectInfo> projectInfos)
			{
				foreach (var projectInfo in projectInfos)
				{
					if (Contains(projectInfo.OutputFilePath)
					// || Contains(projectInfo.OutputRefFilePath)
					)
					{
						return projectInfo;
					}
				}

				return null;
			}

			private ImmutableArray<MetadataReference> GetMetadataReferences()
			{
				var builder = ImmutableArray.CreateBuilder<MetadataReference>();

				for (var index = 0; index < _metadataReferences.Length; index++)
				{
					if (!_indicesToRemove.Contains(index))
					{
						builder.Add(_metadataReferences[index]);
					}
				}

				return builder.ToImmutable();
			}

			private ImmutableArray<ProjectReference> GetProjectReferences()
				=> _projectReferences.ToImmutable();

			public ResolvedReferences ToResolvedReferences()
				=> new ResolvedReferences(GetProjectReferences(), GetMetadataReferences());
		}


		private sealed class MetadataService : IMetadataService
		{
			private readonly MetadataReferenceCache _metadataCache;

			public MetadataService()
			{
				_metadataCache = new MetadataReferenceCache((path, properties) =>
					MetadataReference.CreateFromFile(path, properties));
			}

			public PortableExecutableReference GetReference(string resolvedPath, MetadataReferenceProperties properties)
			{
				return (PortableExecutableReference)_metadataCache.GetReference(resolvedPath, properties);
			}
		}

		private static ImmutableArray<DocumentInfo> CreateDocumentInfos(IReadOnlyList<DocumentFileInfo> documentFileInfos, ProjectId projectId, Encoding encoding)
		{
			var results = ImmutableArray.CreateBuilder<DocumentInfo>();

			foreach (var info in documentFileInfos)
			{
				GetDocumentNameAndFolders(info.LogicalPath, out var name, out var folders);

				var documentInfo = DocumentInfo.Create(
					DocumentId.CreateNewId(projectId, debugName: info.FilePath),
					name,
					folders,
					info.SourceCodeKind,
					new FileTextLoader(info.FilePath, encoding),
					info.FilePath,
					info.IsGenerated);

				results.Add(documentInfo);
			}

			return results.ToImmutable();
		}

		private static readonly char[] s_directorySplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

		private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
		{
			var pathNames = logicalPath.Split(s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);
			if (pathNames.Length > 0)
			{
				if (pathNames.Length > 1)
				{
					folders = pathNames.Take(pathNames.Length - 1).ToImmutableArray();
				}
				else
				{
					folders = ImmutableArray<string>.Empty;
				}

				name = pathNames[pathNames.Length - 1];
			}
			else
			{
				name = logicalPath;
				folders = ImmutableArray<string>.Empty;
			}
		}

		private static string GetAssemblyNameFromProjectPath(string projectFilePath)
		{
			var assemblyName = Path.GetFileNameWithoutExtension(projectFilePath);

			// if this is still unreasonable, use a fixed name.
			if (string.IsNullOrWhiteSpace(assemblyName))
			{
				assemblyName = "assembly";
			}

			return assemblyName;
		}
	}
}
