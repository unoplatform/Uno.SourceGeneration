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
	public interface ISourceGeneratorLogger
	{
		/// <summary>
		/// Log a debugging information.
		/// </summary>
		/// <remarks>
		/// Will appear in build output when the level is set to _normal_.
		/// </remarks>
		/// <example>
		/// logger.Debug($"The count is {count}.");
		/// </example>
		void Debug(IFormattable message, Exception exception = null);
		/// <summary>
		/// Log a debugging information.
		/// </summary>
		/// <remarks>
		/// Will appear in build output when the level is set to _normal_.
		/// </remarks>
		/// <example>
		/// logger.Debug($"The count is {count}.");
		/// </example>
		void Debug(string message, Exception exception = null);

		/// <summary>
		/// Log a useful information to build output.
		/// </summary>
		/// <remarks>
		/// Will appear in build output when the level is set to _detailed_.
		/// </remarks>
		/// <example>
		/// logger.Info($"The count is {count}.");
		/// </example>
		void Info(IFormattable message, Exception exception = null);

		/// <summary>
		/// Log a useful information to build output.
		/// </summary>
		/// <remarks>
		/// Will appear in build output when the level is set to _detailed_.
		/// </remarks>
		/// <example>
		/// logger.Info($"The count is {count}.");
		/// </example>
		void Info(string message, Exception exception = null);

		/// <summary>
		/// Log a WARNING information to build output.
		/// </summary>
		/// <remarks>
		/// Will be reported as a _warning_ in the build result.
		/// </remarks>
		/// <example>
		/// logger.Warn($"The count is {count}.");
		/// </example>
		void Warn(IFormattable message, Exception exception = null);

		/// <summary>
		/// Log a WARNING information to build output.
		/// </summary>
		/// <remarks>
		/// Will be reported as a _warning_ in the build result.
		/// </remarks>
		/// <example>
		/// logger.Warn($"The count is {count}.");
		/// </example>
		void Warn(string message, Exception exception = null);

		/// <summary>
		/// Log an ERROR information to build output.
		/// </summary>
		/// <remarks>
		/// Will be reported as an _error_ in the build result.
		/// </remarks>
		/// <example>
		/// logger.Error($"The count is {count}.", exception);
		/// </example>
		void Error(IFormattable message, Exception exception = null);

		/// <summary>
		/// Log an ERROR information to build output.
		/// </summary>
		/// <remarks>
		/// Will be reported as an _error_ in the build result.
		/// </remarks>
		/// <example>
		/// logger.Error($"The count is {count}.", exception);
		/// </example>
		void Error(string message, Exception exception = null);
	}
}
