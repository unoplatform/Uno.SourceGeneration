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
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Build.Framework;

namespace Uno.SourceGeneratorTasks.Helpers
{
	public class TaskLogger : MarshalByRefObject, Microsoft.Extensions.Logging.ILogger
	{
		private TaskLoggingHelper _taskLog;
        private string _categoryName;

        public TaskLogger(string categoryName)
		{
            _categoryName = categoryName;
		}

        public IDisposable BeginScope<TState>(TState state) => new DisposableAction(() => { });

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Error:
                    TaskLog?.LogError(message);
                    break;

                case LogLevel.Warning:
                    TaskLog?.LogWarning(message);
                    break;

                case LogLevel.Information:
                    TaskLog?.LogMessage(MessageImportance.Normal, message);
                    break;

                case LogLevel.Debug:
                    TaskLog?.LogMessage(MessageImportance.Low, message);
                    break;

                default:
                    TaskLog?.LogMessage(message);
                    break;
            }
        }

        public TaskLoggingHelper TaskLog
        {
            get
            {
                return _taskLog;
            }
            set
            {
                _taskLog = value;
            }
        }
    }
}