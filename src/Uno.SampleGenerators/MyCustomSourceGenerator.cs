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

using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.SourceGeneration;

namespace Uno.SampleGenerators
{
	public class MyCustomSourceGenerator : SourceGenerator
	{
		public override void Execute(SourceGeneratorContext context)
		{
			var project = context.GetProjectInstance();

			context.GetLogger().Debug($"{nameof(MyCustomSourceGenerator)}: This is a DEBUG logging");
			context.GetLogger().Info($"{nameof(MyCustomSourceGenerator)}: This is an INFO logging");

#if DEBUG // Only in DEBUG to prevent breaking the CI build.
			context.GetLogger().Warn($"{nameof(MyCustomSourceGenerator)}: This is a WARN logging - visible only while building in DEBUG");
			context.GetLogger().Error($"{nameof(MyCustomSourceGenerator)}: This is an ERROR logging - visible only while building in DEBUG");
#endif

			var firstCompiledType = context.Compilation.SourceModule.GlobalNamespace.GetTypeMembers().First();

			context.GetLogger().Info($"Double-Click on that should bring you to type {firstCompiledType?.Name} in code.", firstCompiledType);

			context.AddCompilationUnit(
				"Test",
				$@"
namespace Test {{
	public static class MyGeneratedType 
	{{
		// Project: {project?.FullPath}
		public const string Project = @""{ project?.FullPath}"";
	}}
}}"
			);
		}
	}
}
