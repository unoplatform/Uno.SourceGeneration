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
using Uno.SourceGeneration;

namespace Uno.SampleGenerators
{
	public class MyCustomSourceGenerator : SourceGenerator
	{
		private const string DependentTypeName = "Uno.SampleDependency.MyClass";
		private const string LinkedTypeName = "Uno.SampleLinked.LinkedFileClass";

		public override void Execute(SourceGeneratorContext context)
		{
			var project = context.GetProjectInstance();

			context.GetLogger().Debug($"{nameof(MyCustomSourceGenerator)}: This is a DEBUG logging");
			context.GetLogger().Info($"{nameof(MyCustomSourceGenerator)}: This is an INFO logging");

#if DEBUG // Only in DEBUG to prevent breaking the CI build.
			context.GetLogger().Warn($"{nameof(MyCustomSourceGenerator)}: This is a WARN logging");
			context.GetLogger().Error($"{nameof(MyCustomSourceGenerator)}: This is an ERROR logging");
#endif

			// This test ensures that dependent libraries are included in the compilation
			// generated from the AdHoc workspace.
			var dependentString = BuildVariableFromType(context, DependentTypeName, "_dependent");

			// This test ensures that linked files included in the project are included
			// in the Compilation instance used by the generators.
			var linkedString = BuildVariableFromType(context, LinkedTypeName, "_linked");

			context.AddCompilationUnit(
				"Test",
				$@"
#pragma warning disable 169
namespace Test {{
	public static class MyGeneratedType 
	{{
		// Project: {project?.FullPath}
		public const string Project = @""{ project?.FullPath}"";
		{dependentString}
		{linkedString}
	}}
}}"
			);
		}

		private static string BuildVariableFromType(SourceGeneratorContext context, string typeName, string variable)
			=> context.Compilation.GetTypeByMetadataName(typeName) is INamedTypeSymbol symbol
				? $"static {symbol} {variable};"
				: $"#error type {typeName} is not available";
	}
}
