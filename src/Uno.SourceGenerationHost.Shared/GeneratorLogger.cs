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
using Microsoft.Extensions.Logging;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host
{
	public class GeneratorLogger : ISourceGeneratorLogger
	{
		private readonly ILogger _logger;

		public GeneratorLogger(ILogger logger)
		{
			_logger = logger;
		}

		public void Debug(IFormattable message, Exception exception = null)
		{
			_logger.Debug(() => message?.ToString(), exception);
		}

		public void Info(IFormattable message, Exception exception = null)
		{
			_logger.Info(() => message?.ToString(), exception);
		}

		public void Warn(IFormattable message, Exception exception = null)
		{
			_logger.Warn(() => message?.ToString(), exception);
		}

		public void Error(IFormattable message, Exception exception = null)
		{
			_logger.Error(() => message?.ToString(), exception);
		}
	}
}
