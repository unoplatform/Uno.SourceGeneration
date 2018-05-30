using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web.UI.Design;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.SourceGeneration.Intellisense
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	public sealed class SourceGenerationIntellisenseFixerPackage : Package
	{
		private DTE _dte;
		private IVsSolution _solution;
		private Events _events;
		private BuildEvents _buildEvents;
		private DocumentEvents _documentEvents;

		private Window _outputWindow;
		private OutputWindowPane _outputPane;

		public const string PackageGuidString = "04912a0c-bf5d-4e71-9e04-5d5d3309e781";
		private const string SolutionFolderItemKind = "{66A26722-8FB5-11D2-AA7E-00C04F688DDE}";
		private const string FixintellisenseProperty = "UnoSourceGeneration_FixIntellisense";
		private const string TempFolderName = "__temp-to-delete";

		protected override void Initialize()
		{
			_dte = (DTE)GetService(typeof(DTE));
			_solution = (IVsSolution)GetService(typeof(SVsSolution));

			_events = _dte.Events;
			_buildEvents = _events.BuildEvents;
			_documentEvents = _events.DocumentEvents;

			_documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
			_buildEvents.OnBuildDone += BuildEventsOnOnBuildDone;

			_outputWindow = _dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
			_outputPane = ((OutputWindow)_outputWindow.Object).OutputWindowPanes.Add("Uno.SourceGeneration - Intellisense Fixer");

			base.Initialize();
		}

		private ConcurrentBag<Document> _changedDocuments = new ConcurrentBag<Document>();

		private void DocumentEventsOnDocumentSaved(Document document)
		{
			var itemType = document
				?.ProjectItem
				?.Properties
				?.Item("ItemType")
				?.Value as string;

			if (itemType == "Compile")
			{
				_changedDocuments.Add(document);
			}
		}

		private void BuildEventsOnOnBuildDone(vsBuildScope scope, vsBuildAction action)
		{
			switch (action)
			{
				case vsBuildAction.vsBuildActionBuild:
				case vsBuildAction.vsBuildActionRebuildAll:
					break;
				default:
					return; // not interesting
			}

			Log("\t---");
			Log($"{action} detected. Checking for projects to fix...");

			var projectsUsingCodeGen = GetProjectsUsingCodeGen().ToArray();

			if (projectsUsingCodeGen.Length == 0)
			{
				Log($"No project with property <{FixintellisenseProperty}>true</{FixintellisenseProperty}> defined in csproj.");
				return;
			}

			foreach (var project in projectsUsingCodeGen)
			{
				try
				{
					var projectFolder = Path.GetDirectoryName(project.FileName);
					var folderPath = Path.Combine(projectFolder, TempFolderName);

					var folderItem = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(i => i.Name.Equals(TempFolderName, StringComparison.OrdinalIgnoreCase));
					if (folderItem == null)
					{
						if (Directory.Exists(folderPath))
						{
							Log($"Deleting local folder {folderPath} for {project.Name}.");
							Directory.Delete(folderPath);
						}

						Log($"Fixing project {project.Name} by creating a temp folder...");
						folderItem = project.ProjectItems.AddFolder(TempFolderName);
					}
					else
					{
						Log($"Fixing folder already exists in project {project.Name}. Simply deleting it.");
					}
					folderItem.Remove();
					if (Directory.Exists(folderPath))
					{
						Directory.Delete(folderPath);
					}
					Log($"Temp folder {folderItem.Name} removed successfully.");
				}
				catch (Exception ex)
				{
					Log("-------------------------------------");
					Log($"Error fixing project {project.Name}:");
					Log(ex.ToString());
					Log("-------------------------------------");
				}
			}
			Log("Finished applying fix for intellisense.");
		}

		private void Log(string str)
		{
			_outputPane.OutputString(str);
			_outputPane.OutputString("\r\n");
		}

		private IEnumerable<Project> GetProjectsUsingCodeGen(Project[] projects = null)
		{
			projects = projects ?? _dte.Solution.Projects.OfType<Project>().ToArray();

			foreach (var project in projects)
			{
				_solution.GetProjectOfUniqueName(project.FullName, out var hierarchy);

				if (hierarchy is IVsBuildPropertyStorage storage)
				{
					storage.GetPropertyValue(FixintellisenseProperty, string.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, out var value);

					if (!string.IsNullOrWhiteSpace(value) &&
						bool.TryParse(value, out var fixIntellisense) &&
						fixIntellisense)
					{
						yield return project;
					}
				}

				var subProjects = project.ProjectItems
					.OfType<ProjectItem>()
					.Where(p=> p.Kind.Equals(SolutionFolderItemKind, StringComparison.OrdinalIgnoreCase))
					.Select(p => p.SubProject)
					.Where(x => x != null)
					.ToArray();

				if (subProjects.Length > 0)
				{
					foreach (var subProject in GetProjectsUsingCodeGen(subProjects))
					{
						yield return subProject;
					}
				}
			}
		}

		//private static 
	}
}
