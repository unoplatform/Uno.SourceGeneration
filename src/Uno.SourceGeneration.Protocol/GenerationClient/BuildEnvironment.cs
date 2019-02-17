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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Uno.SourceGeneratorTasks
{
	[Serializable]
	[DataContract]
	public class BuildEnvironment
	{
		[DataMember]
		public string Configuration { get; set; }
		[DataMember]
		public string Platform { get; set; }
		[DataMember]
		public string ProjectFile { get; set; }
		[DataMember]
		public string OutputPath { get; set; }
		[DataMember]
		public string TargetFramework { get; set; }
		[DataMember]
		public string VisualStudioVersion { get; set; }
		[DataMember]
		public string TargetFrameworkRootPath { get; set; }
		[DataMember]
		public string BinLogOutputPath { get; set; }
		[DataMember]
		public bool BinLogEnabled { get; set; }
		[DataMember]
		public string MSBuildBinPath { get; set; }
		[DataMember]
		public string[] AdditionalAssemblies { get; set; }
		[DataMember]
		public string[] SourceGenerators { get; set; }
		[DataMember]
		public string[] ReferencePath { get; set; }
	}
}
