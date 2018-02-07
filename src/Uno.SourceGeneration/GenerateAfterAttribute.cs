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
	/// Defines a dependency between source generators
	/// </summary>
	/// <remarks>
	/// Can be defined more than once for a generator.
	/// No effect when generator is not found.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class GenerateAfterAttribute : Attribute
	{
		/// <summary>
		/// Fully Qualified Name (FQN: namespace + class name) of the generator to execute before.
		/// </summary>
		/// <remarks>
		/// No effect if the generator is not found.
		/// </remarks>
		public string GeneratorToExecuteBefore { get; }

		/// <summary>
		/// Defines a dependency between source generators
		/// </summary>
		/// <param name="generatorToExecuteBefore">
		/// Fully Qualified Name (FQN: namespace + class name) of the generator to execute before
		/// </param>
		public GenerateAfterAttribute(string generatorToExecuteBefore)
		{
			GeneratorToExecuteBefore = generatorToExecuteBefore;
		}
	}
}
