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

namespace Uno.SourceGeneration.Host
{
	/// <summary>
	/// Minimal <see cref="ILoggerProvider"/> writing log messages to <see cref="Console"/>.
	/// Used in place of the legacy <c>ConsoleLoggerProvider((t, l) => true, true)</c> constructor
	/// which was removed in Microsoft.Extensions.Logging 2.0+.
	/// </summary>
	internal sealed class HostConsoleLoggerProvider : ILoggerProvider
	{
		private readonly LogLevel _minLevel;

		public HostConsoleLoggerProvider(LogLevel minLevel = LogLevel.Trace)
		{
			_minLevel = minLevel;
		}

		public ILogger CreateLogger(string categoryName) => new HostConsoleLogger(categoryName, _minLevel);

		public void Dispose()
		{
		}

		private sealed class HostConsoleLogger : ILogger
		{
			private readonly string _categoryName;
			private readonly LogLevel _minLevel;

			public HostConsoleLogger(string categoryName, LogLevel minLevel)
			{
				_categoryName = categoryName;
				_minLevel = minLevel;
			}

			public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

			public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && logLevel != LogLevel.None;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
			{
				if (!IsEnabled(logLevel))
				{
					return;
				}

				var message = formatter != null ? formatter(state, exception) : state?.ToString();
				if (string.IsNullOrEmpty(message) && exception == null)
				{
					return;
				}

				var writer = logLevel >= LogLevel.Error ? Console.Error : Console.Out;
				writer.WriteLine($"{logLevel}: {_categoryName}: {message}");

				if (exception != null)
				{
					writer.WriteLine(exception);
				}
			}

			private sealed class NullScope : IDisposable
			{
				public static readonly NullScope Instance = new NullScope();
				public void Dispose() { }
			}
		}
	}
}
