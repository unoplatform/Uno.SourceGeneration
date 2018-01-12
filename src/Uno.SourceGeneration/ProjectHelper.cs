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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.SourceGeneration
{
	public static class SourceGeneratorExtensions
	{
		private static ConditionalWeakTable<SourceGeneratorContext, ProjectInstance> _project = new ConditionalWeakTable<SourceGeneratorContext, ProjectInstance>();

		public static ProjectInstance GetProjectInstance(this SourceGeneratorContext context)
		{
			ProjectInstance instance;

			if(!_project.TryGetValue(context, out instance))
			{
				throw new InvalidOperationException("The SourceGeneratorContext has not been initialized from a SourceGeneratorHost.");
			}

			return instance;
		}

		public static void SetProjectInstance(this SourceGeneratorContext context, ProjectInstance projectInstance)
		{
			_project.Add(context, projectInstance);
		}
	}
}
