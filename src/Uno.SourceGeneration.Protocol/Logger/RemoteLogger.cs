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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneratorTasks.Logger
{
    public class RemoteLogger : MarshalByRefObject, Microsoft.Extensions.Logging.ILogger
    {
        private RemotableLogger2 _taskLog;
        private string _loggerName;

        public RemoteLogger(string loggerName)
        {
            _loggerName = loggerName;
        }

		public override object InitializeLifetimeService()
		{
			// Keep this object alive infinitely, it will be deleted along with the 
			// host msbuild.exe process.
			return null;
		}

		public RemotableLogger2 TaskLog
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

        public IDisposable BeginScope<TState>(TState state) => new DisposableAction(() => { });

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _taskLog?.WriteLog((int)logLevel, message);
        }
    }
}
