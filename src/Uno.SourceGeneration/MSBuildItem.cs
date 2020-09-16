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
using Microsoft.Build.Execution;

namespace Uno.SourceGeneration
{
	/// <summary>
	/// Defines an Item provided by MSBuild
	/// </summary>
	public class MSBuildItem
	{
		private ProjectItemInstance _projectItemInstance;

		internal MSBuildItem(ProjectItemInstance v)
		{
			_projectItemInstance = v;
			Identity = v.EvaluatedInclude;
		}

		/// <summary>
		/// Gets the Identity (EvalutatedInclude) of the item
		/// </summary>
		public string Identity { get; }

		/// <summary>
		/// Gets a metadata for this item
		/// </summary>
		/// <param name="name">The name of the metadata</param>
		/// <returns>The metadata value</returns>
		public string GetMetadataValue(string name)
			=> _projectItemInstance.GetMetadataValue(name);
	}
}
