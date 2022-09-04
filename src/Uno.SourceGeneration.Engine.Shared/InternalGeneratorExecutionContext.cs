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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Uno.SourceGeneration.Host
{
	internal class InternalGeneratorExecutionContext : GeneratorExecutionContext
	{
		private ConcurrentBag<KeyValuePair<string, string>> _trees = new ConcurrentBag<KeyValuePair<string, string>>();

		public InternalGeneratorExecutionContext(Compilation compilation, ParseOptions parseOptions, CancellationToken token, Project project)
			: base(compilation, parseOptions, token)
		{
			Project = project;
		}

		public override void AddSource(string hintName, string source)
		{
			_trees.Add(new KeyValuePair<string, string>(hintName, source));
		}

		public override void AddSource(string hintName, SourceText sourceText)
		{
			_trees.Add(new KeyValuePair<string, string>(hintName, sourceText.ToString()));
		}

		public override void ReportDiagnostic(Diagnostic diagnostic)
		{
		}

		internal IEnumerable<KeyValuePair<string, string>> Trees => _trees;

		internal Project Project { get; private set; }

		public override ImmutableArray<AdditionalText> AdditionalFiles => Project.AnalyzerOptions.AdditionalFiles;
	}
}
