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
using System.Text;
using Uno.SourceGeneration;

namespace Uno.SampleGenerators
{
	[Generator]
	public class AdditionalFilesGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{

		}

		public void Execute(GeneratorExecutionContext context)
		{
			var count = context.AdditionalFiles.Length;
			var dictionaryStringBuilder = new StringBuilder();
			for (int i = 0; i < count; i++)
			{
				var additionalFile = context.AdditionalFiles[i];
				if (!context.TryGetOptionValue(additionalFile, "build_metadata.AdditionalFiles.MyOption", out var myOptionValue))
				{
					myOptionValue = "Not found :(";
				}

				dictionaryStringBuilder.Append($@"
							{{ ""{additionalFile.Path.Replace("\\", "\\\\")}"", ""{additionalFile.GetText()} -- MyOption: {myOptionValue}"" }},
");
			}
			context.AddSource(
				"AdditionalFiles",
				$@"
				using System.Collections.Generic;

				namespace AdditionalFilesGenerator {{
					public static class AdditionalFilesInfo
					{{
						public const int Count = {count};

						public static Dictionary<string, string> Files = new Dictionary<string, string>()
						{{
{dictionaryStringBuilder}
						}};
					}}
				}}");
		}
	}
}
