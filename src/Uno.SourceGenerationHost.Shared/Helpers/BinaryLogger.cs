using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Helpers
{
	internal class BinaryLoggerForwarder : Microsoft.Extensions.Logging.ILogger
	{
		private readonly BinaryLoggerEventSource _eventSource;
		private readonly string _categoryName;

		public BinaryLoggerForwarder(string categoryName, BinaryLoggerEventSource eventSource)
		{
			_eventSource = eventSource;
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
					_eventSource.RaiseError(_categoryName, message);
					break;

				case LogLevel.Warning:
					_eventSource.RaiseWarning(_categoryName, message);
					break;

				case LogLevel.Information:
					_eventSource.RaiseMessage(_categoryName, message, MessageImportance.Normal);
					break;

				case LogLevel.Debug:
					_eventSource.RaiseMessage(_categoryName, message, MessageImportance.Low);
					break;

				default:
					_eventSource.RaiseMessage(_categoryName, message, MessageImportance.Low);
					break;
			}
		}

		public void Dispose() => throw new NotImplementedException();

	}
}
