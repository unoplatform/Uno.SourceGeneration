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

namespace Uno.SourceGeneratorTasks
{
	[Serializable]
	public class BuildEnvironment
	{
		public string Configuration { get; }
		public string Platform { get; }
		public string ProjectFile { get; }
		public string OutputPath { get; }
		public string TargetFramework { get; }
		public string VisualStudioVersion { get; }
		public string TargetFrameworkRootPath { get; }
		public string BinLogOutputPath { get; }
		public bool BinLogEnabled { get; }

		public BuildEnvironment(
			string configuration,
			string platform,
			string projectFile,
			string outputPath,
			string targetFramework,
			string visualStudioVersion,
			string targetFrameworkRootPath,
			string binLogOutputPath,
			bool binLogEnabled
		)
		{
			Configuration = configuration;
			Platform = platform;
			ProjectFile = projectFile;
			OutputPath = outputPath;
			TargetFramework = targetFramework;
			VisualStudioVersion = visualStudioVersion;
			TargetFrameworkRootPath = targetFrameworkRootPath;
			BinLogOutputPath = binLogOutputPath;
			BinLogEnabled = binLogEnabled;
		}
	}
}
