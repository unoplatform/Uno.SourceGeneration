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

namespace Uno.SourceGeneration
{
	/// <summary>
	/// Defines a dependent source generators, which should be executed after
	/// </summary>
	/// <remarks>
	/// Can be defined more than once for a generator.
	/// No effect when generator is not found.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class GenerateBeforeAttribute : Attribute
	{
		/// <summary>
		/// Fully Qualified Name (FQN: namespace + class name) of the generator to execute after.
		/// </summary>
		/// <remarks>
		/// No effect if the generator is not found.
		/// </remarks>
		public string GeneratorToExecuteAfter { get; }

		/// <summary>
		/// Defines a dependent source generators, which should be executed after
		/// </summary>
		/// <param name="generatorToExecuteAfter">
		/// Fully Qualified Name (FQN: namespace + class name) of the generator to execute after.
		/// </param>
		public GenerateBeforeAttribute(string generatorToExecuteAfter)
		{
			GeneratorToExecuteAfter = generatorToExecuteAfter;
		}
	}
}