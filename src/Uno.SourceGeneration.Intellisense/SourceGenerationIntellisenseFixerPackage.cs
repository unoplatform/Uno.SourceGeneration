using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.SourceGeneration.Intellisense
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists)]
	public sealed class SourceGenerationIntellisenseFixerPackage : Package
	{
		private DTE _dte;
		private Events _events;
		private BuildEvents _buildEvents;
		private DocumentEvents _documentEvents;

		public const string PackageGuidString = "04912a0c-bf5d-4e71-9e04-5d5d3309e781";

		protected override void Initialize()
		{
			_dte = GetService(typeof(DTE)) as DTE;

			_events = _dte.Events;
			_buildEvents = _events.BuildEvents;
			_documentEvents = _events.DocumentEvents;

			_documentEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
			_buildEvents.OnBuildDone += BuildEventsOnOnBuildDone;

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

			var documents = Interlocked.Exchange(ref _changedDocuments, new ConcurrentBag<Document>());

			// Fixing only one document per project is enough
			var oneDocumentPerProject = documents
				.GroupBy(d => d.ProjectItem.ContainingProject)
				.Select(p => p.FirstOrDefault(d => File.Exists(d.FullName))) // we want non-deleted documents
				.Where(d => d != null); // prevent NRE

			foreach (var document in oneDocumentPerProject)
			{
				var property= document.ProjectItem.Properties.Item("ItemType");
				var previousValue = property.Value as string;
				if (previousValue == "Compile")
				{
					property.Value = "None";
					property.Value = previousValue;
				}

				// Force save project to avoid annoying the user about saving it
				document.ProjectItem.ContainingProject.Save();
			}
		}
	}
}
