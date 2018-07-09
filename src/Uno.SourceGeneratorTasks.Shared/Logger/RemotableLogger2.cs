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
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.SourceGeneratorTasks.Logger
{
    public class RemotableLogger2 : MarshalByRefObject
    {
        private readonly Microsoft.Extensions.Logging.ILogger _log;

        public RemotableLogger2(Microsoft.Extensions.Logging.ILogger log)
        {
            _log = log;
        }

		public override object InitializeLifetimeService()
		{
			// Keep this object alive infinitely, it will be deleted along with the 
			// host msbuild.exe process.
			return null;
		}

		public void WriteLog(int logLevel, string message)
        {
			try
			{
				_log.Log<object>((Microsoft.Extensions.Logging.LogLevel)logLevel, 0, null, null, (_, __) => message);
			}
			catch(Exception /*e*/)
			{
				// This may happen under MacOS, where mono's remoting fails when calling _log methods.
				// We can fallback on console logging until it's fixed.
				System.Console.WriteLine(message);
			}
        }
    }
}
