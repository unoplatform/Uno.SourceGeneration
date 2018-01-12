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
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Uno.SourceGeneration;
using System.Collections.Concurrent;

namespace Uno.SourceGeneration.Host
{
	internal class InternalSourceGeneratorContext : SourceGeneratorContext
	{
		private ConcurrentBag<KeyValuePair<string, string>> _trees = new ConcurrentBag<KeyValuePair<string, string>>();

		public InternalSourceGeneratorContext(Compilation compilation, Project project)
		{
			Compilation = compilation;
			Project = project;
		}

		public IEnumerable<KeyValuePair<string, string>> Trees => _trees;

		public override Compilation Compilation { get; }

		public override Project Project { get; }

		public override void AddCompilationUnit(string name, string tree)
		{
			_trees.Add(new KeyValuePair<string, string>(name, tree));
		}

		public override void AddCompilationUnit(string name, SyntaxTree tree)
		{
			_trees.Add(new KeyValuePair<string, string>(name, tree.ToString()));
		}

		public override void ReportDiagnostic(Diagnostic diagnostic)
		{

		}
	}
}