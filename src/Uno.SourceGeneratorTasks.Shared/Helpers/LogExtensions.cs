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
using System.Globalization;
using Uno.SourceGeneratorTasks.Shared.Helpers;

namespace Uno.SourceGeneratorTasks.Helpers
{
	public static partial class LogExtensions
	{
		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsDebugEnabled
		/// first to prevent useless processing constructing a logging message.
		/// </remarks>
		public static void DebugFormat(this ILogger log, object message)
		{
			log.Log<CodeSpan>(LogLevel.Debug, 0, null, null, (_, __) => message?.ToString());
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsDebugEnabled
		/// first to prevent useless processing constructing a logging message.
		/// </remarks>
		public static void DebugFormat(this ILogger log, string format, object arg0)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0);
			log.Log<CodeSpan>(LogLevel.Debug, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsDebugEnabled
		/// first to prevent useless processing constructing a logging message.
		/// </remarks>
		public static void DebugFormat(this ILogger log, string format, object arg0, object arg1)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
			log.Log<CodeSpan>(LogLevel.Debug, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsDebugEnabled
		/// first to prevent useless processing constructing a logging message.
		/// </remarks>
		public static void DebugFormat(this ILogger log, string format, object arg0, object arg1, object arg2)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
			log.Log<CodeSpan>(LogLevel.Debug, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsDebugEnabled
		/// first to prevent useless processing constructing a logging message.
		/// </remarks>
		public static void DebugFormat(this ILogger log, string format, params object[] args)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, args);
			log.Log<CodeSpan>(LogLevel.Debug, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers.
		/// </summary>
		public static void Debug(this ILogger log, string message, Exception exception = null, CodeSpan span = null)
		{
			log.Log<CodeSpan>(LogLevel.Debug, 0, span, exception, (_, __) => message);
		}

		/// <summary>
		/// Send a "Debug" message to configured loggers using a deferred action to build the message,
		/// if the Debug log level is enabled.
		/// </summary>
		public static void Debug(this ILogger log, Func<object> messageBuilder, Exception exception = null, CodeSpan span = null)
		{
			if (log.IsEnabled(LogLevel.Debug))
			{
				log.Log(LogLevel.Debug, 0, span, exception, (_, __) => messageBuilder()?.ToString());
			}
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsInfoEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Info" is usually always available.
		/// </remarks>
		public static void InfoFormat(this ILogger log, object message)
		{
			log.Log<CodeSpan>(LogLevel.Information, 0, null, null, (_, __) => message?.ToString());
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsInfoEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Info" is usually always available.
		/// </remarks>
		public static void InfoFormat(this ILogger log, string format, object arg0)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0);
			log.Log<CodeSpan>(LogLevel.Information, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsInfoEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Info" is usually always available.
		/// </remarks>
		public static void InfoFormat(this ILogger log, string format, object arg0, object arg1)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
			log.Log<CodeSpan>(LogLevel.Information, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsInfoEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Info" is usually always available.
		/// </remarks>
		public static void InfoFormat(this ILogger log, string format, object arg0, object arg1, object arg2)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
			log.Log<CodeSpan>(LogLevel.Information, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsInfoEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Info" is usually always available.
		/// </remarks>
		public static void InfoFormat(this ILogger log, string format, params object[] args)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, args);
			log.Log<CodeSpan>(LogLevel.Information, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Info" message to configured loggers.
		/// </summary>
		public static void Info(this ILogger log, string message, Exception exception = null, CodeSpan span = null)
		{
			log.Log(LogLevel.Information, 0, span, exception, (_, __) => message);
		}

		/// <summary>
		/// Send a "Info" message to configured loggers using a deferred action to build the message,
		/// if the Info log level is enabled.
		/// </summary>
		public static void Info(this ILogger log, Func<object> messageBuilder, Exception exception = null, CodeSpan span = null)
		{
			if (log.IsEnabled(LogLevel.Information))
			{
				log.Log(LogLevel.Information, 0, span, exception, (_, __) => messageBuilder()?.ToString());
			}
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsWarnEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Warn" is usually always available.
		/// </remarks>
		public static void WarnFormat(this ILogger log, object message)
		{
			log.Log<CodeSpan>(LogLevel.Warning, 0, null, null, (_, __) => message?.ToString());
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsWarnEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Warn" is usually always available.
		/// </remarks>
		public static void WarnFormat(this ILogger log, string format, object arg0)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0);
			log.Log<CodeSpan>(LogLevel.Warning, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsWarnEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Warn" is usually always available.
		/// </remarks>
		public static void WarnFormat(this ILogger log, string format, object arg0, object arg1)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
			log.Log<CodeSpan>(LogLevel.Warning, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsWarnEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Warn" is usually always available.
		/// </remarks>
		public static void WarnFormat(this ILogger log, string format, object arg0, object arg1, object arg2)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
			log.Log<CodeSpan>(LogLevel.Warning, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsWarnEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Warn" is usually always available.
		/// </remarks>
		public static void WarnFormat(this ILogger log, string format, params object[] args)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, args);
			log.Log<CodeSpan>(LogLevel.Warning, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers using a deferred action to build the message,
		/// if the Warn log level is enabled.
		/// </summary>
		public static void Warn(this ILogger log, Func<object> messageBuilder, Exception exception = null, CodeSpan span = null)
		{
			if (log.IsEnabled(LogLevel.Warning))
			{
				log.Log(LogLevel.Warning, 0, span, exception, (_, __) => messageBuilder()?.ToString());
			}
		}

		/// <summary>
		/// Send a "Warn" message to configured loggers.
		/// </summary>
		public static void Warn(this ILogger log, string message, Exception exception = null, CodeSpan span = null)
		{
			log.Log(LogLevel.Warning, 0, span, exception, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsErrorEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Error" is usually always available.
		/// </remarks>
		public static void ErrorFormat(this ILogger log, object message)
		{
			log.Log<CodeSpan>(LogLevel.Error, 0, null, null, (_, __) => message?.ToString());
		}

		/// <summary>
		/// Send a "Error" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsErrorEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Error" is usually always available.
		/// </remarks>
		public static void ErrorFormat(this ILogger log, string format, object arg0)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0);
			log.Log<CodeSpan>(LogLevel.Error, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsErrorEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Error" is usually always available.
		/// </remarks>
		public static void ErrorFormat(this ILogger log, string format, object arg0, object arg1)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1);
			log.Log<CodeSpan>(LogLevel.Error, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsErrorEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Error" is usually always available.
		/// </remarks>
		public static void ErrorFormat(this ILogger log, string format, object arg0, object arg1, object arg2)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2);
			log.Log<CodeSpan>(LogLevel.Error, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers.
		/// </summary>
		/// <remarks>
		/// If the construction of the message is costly, you should check the .IsErrorEnabled
		/// first to prevent useless processing constructing a logging message.
		/// Note: "Error" is usually always available.
		/// </remarks>
		public static void ErrorFormat(this ILogger log, string format, params object[] args)
		{
			var message = string.Format(CultureInfo.InvariantCulture, format, args);
			log.Log<CodeSpan>(LogLevel.Error, 0, null, null, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers using a deferred action to build the message,
		/// if the Error log level is enabled.
		/// </summary>
		public static void Error(this ILogger log, string message, Exception exception = null, CodeSpan span = null)
		{
			log.Log(LogLevel.Error, 0, span, exception, (_, __) => message);
		}

		/// <summary>
		/// Send a "Error" message to configured loggers using a deferred action to build the message,
		/// if the Error log level is enabled.
		/// </summary>
		public static void Error(this ILogger log, Func<object> messageBuilder, Exception exception = null, CodeSpan span = null)
		{
			if (log.IsEnabled(LogLevel.Error))
			{
				log.Log(LogLevel.Error, 0, span, exception, (_, __) => (messageBuilder()?.ToString()));
			}
		}
	}
}
