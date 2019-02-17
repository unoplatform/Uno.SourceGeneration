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
using System;
using System.IO;
using Microsoft.Build.Execution;
using System.Linq;
using Microsoft.Build.Evaluation;
using Uno.SourceGeneration.Host.Helpers;

namespace Uno.SourceGeneration.Host
{
	public class ProjectDetails : IDisposable
	{
		private (string, DateTime)[] _timeStamps;

		public string Configuration { get; internal set; }
		public ProjectInstance ExecutedProject { get; internal set; }
		public (Type generatorType, Func<SourceGenerator> builder)[] Generators { get; internal set; }
		public string IntermediatePath { get; internal set; }
		public Project LoadedProject { get; internal set; }
		public string[] References { get; internal set; }
		public ProjectCollection Collection { get; internal set; }

		public void BuildImportsMap()
		{
			_timeStamps = LoadedProject
				.Imports
				.Select(i => i.ImportedProject.FullPath)
				.Concat(new[] { ExecutedProject.FullPath })
				.Concat(ExecutedProject.GetItems("Compile").Select(GetItemFullPath))
				.Concat(ExecutedProject.GetItems("EmbeddedResources").Select(GetItemFullPath))
				.Concat(ExecutedProject.GetItems("UpToDateCheckInput").Select(GetItemFullPath))
				.Where(p => p != null)
				.Distinct()
				.Select(p => (p, File.GetLastWriteTime(p)))
				.ToArray();
		}

		private string GetItemFullPath(ProjectItemInstance item)
			=> FileUtilities.ResolveRelativePath(item.EvaluatedInclude, ExecutedProject.Directory) ?? item.EvaluatedInclude;

		public void Dispose()
		{
			Collection?.Dispose();
		}

		public bool HasChanged()
		{
			var updatedStamps = _timeStamps
			.Select(t => File.Exists(t.Item1) ? File.GetLastWriteTime(t.Item1) : default(DateTime));

			return !updatedStamps.SequenceEqual(_timeStamps.Select(t => t.Item2));
		}
	}

}
