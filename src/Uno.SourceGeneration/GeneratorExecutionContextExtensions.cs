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
	public static class GeneratorExecutionContextExtensions
	{
		private static readonly ConditionalWeakTable<GeneratorExecutionContext, ProjectInstance> _project
			= new ConditionalWeakTable<GeneratorExecutionContext, ProjectInstance>();

		private static readonly ConditionalWeakTable<GeneratorExecutionContext, ISourceGeneratorLogger> _logger
			= new ConditionalWeakTable<GeneratorExecutionContext, ISourceGeneratorLogger>();

		public static ProjectInstance GetProjectInstance(this GeneratorExecutionContext context)
		{
			if (_project.TryGetValue(context, out var instance))
			{
				return instance;
			}

			throw new InvalidOperationException("The SourceGeneratorContext has not been initialized from a SourceGeneratorHost.");
		}

		public static void SetProjectInstance(this GeneratorExecutionContext context, ProjectInstance projectInstance)
		{
			_project.Add(context, projectInstance);
		}

		public static ISourceGeneratorLogger GetLogger(this GeneratorExecutionContext context)
		{
			if (_logger.TryGetValue(context, out var logger))
			{
				return logger;
			}

			throw new InvalidOperationException("The SourceGeneratorContext has not been initialized from a SourceGeneratorHost.");
		}

		public static void SetLogger(this GeneratorExecutionContext context, ISourceGeneratorLogger logger)
		{
			_logger.Add(context, logger);
		}

		/// <summary>
		/// Gets the value of an MSBuild property
		/// </summary>
		/// <param name="context">The generator context</param>
		/// <param name="name">The name of the property</param>
		/// <param name="defaultValue">The default value if the string is null or empty</param>
		/// <returns></returns>
		public static string GetMSBuildPropertyValue(
			this GeneratorExecutionContext context,
			string name,
			string defaultValue = "")
		{
			var value = GetProjectInstance(context).GetPropertyValue(name);
			return string.IsNullOrEmpty(value) ? defaultValue : value;
		}

		/// <summary>
		/// Gets an enumerable of <see cref="MSBuildItem"/> from the MSBuild context
		/// </summary>
		/// <param name="context">The generator context</param>
		/// <param name="name">The name of the MSBuild item</param>
		/// <returns>An enumerable</returns>
		public static IEnumerable<MSBuildItem> GetMSBuildItems(this GeneratorExecutionContext context, string name)
		{
			var value = GetProjectInstance(context).GetItems(name);
			return value.Select(v => new MSBuildItem(v));
		}
	}
}
