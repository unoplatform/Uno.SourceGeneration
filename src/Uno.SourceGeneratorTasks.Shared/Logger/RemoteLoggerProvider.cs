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

namespace Uno.SourceGeneratorTasks.Logger
{
    public class RemoteLoggerProvider : ILoggerProvider
    {
        private List<RemoteLogger> _loggers = new List<RemoteLogger>();
        private RemotableLogger2 _taskLog;

        public RemotableLogger2 TaskLog
        {
            get { return _taskLog; }
            set
            {
                _taskLog = value;

                foreach(var logger in _loggers)
                {
                    logger.TaskLog = value;
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new RemoteLogger(categoryName) {  TaskLog = _taskLog };

            _loggers.Add(logger);

            return logger;
        }

        public void Dispose()
        {

        }
    }
}
