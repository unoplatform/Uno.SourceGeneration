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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Uno.SourceGeneration
{

	/// <summary>
	/// Defines a C# 9.0 compatible generator execution context
	/// </summary>
	public abstract class GeneratorExecutionContext
	{
		protected GeneratorExecutionContext(Compilation compilation, ParseOptions parseOptions, CancellationToken token)
		{
			Compilation = compilation;
			ParseOptions = parseOptions;
			CancellationToken = token;
		}

		public Compilation Compilation { get; }

		/// <summary>
		/// Unused, present for compatibilty
		/// </summary>
		public ParseOptions ParseOptions { get; }

		/// <summary>
		/// Unused, present for compatibilty
		/// </summary>
		public abstract ImmutableArray<AdditionalText> AdditionalFiles { get; }

		public CancellationToken CancellationToken { get; }

		/// <summary>
		/// Adds a generated source file
		/// </summary>
		public abstract void AddSource(string hintName, string source);

		/// <summary>
		/// Adds a generated source file
		/// </summary>
		public abstract void AddSource(string hintName, SourceText sourceText);

		/// <summary>
		/// Unused, present for compatibilty
		/// </summary>
		public abstract void ReportDiagnostic(Diagnostic diagnostic);

		public abstract bool TryGetOptionValue(SyntaxTree tree, string key, out string value);

		public abstract bool TryGetOptionValue(AdditionalText textFile, string key, out string value);

		public abstract bool TryGetGlobalOptionValue(string key, out string value);

	}
}
