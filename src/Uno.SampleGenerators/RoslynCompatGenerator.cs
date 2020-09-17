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
using Uno.SourceGeneration;

namespace Uno.SampleGenerators
{
	[GenerateAfter("Uno.SampleGenerators.MyCustomSourceGenerator")]
	[Generator]
	public class RoslynCompatGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{

		}

		public void Execute(GeneratorExecutionContext context)
		{
			context.AddSource(
				"Test3",
				$@"
				namespace RoslynCompatGeneratorTest {{
					public static class TestType
					{{
						// reusing the compiled code form other generator
						public const int Project = {context.GetMSBuildPropertyValue("MyTestProperty")};
					}}
				}}");
		}
	}
}
