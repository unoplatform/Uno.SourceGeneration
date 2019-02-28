using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Helpers
{
	/// <summary>
	/// A Microsoft.Extensions.Logging logger forwarder to Microsoft.Build.Logging.BinaryLogger.
	/// </summary>
	internal class BinaryLoggerForwarderProvider : ILoggerProvider, IDisposable
	{
		private readonly BinaryLoggerEventSource _source;
		private readonly Microsoft.Build.Logging.BinaryLogger _msbuildLogger;

		public BinaryLoggerForwarderProvider(string outputFilePath)
		{
			_source = new BinaryLoggerEventSource();

			_msbuildLogger = new Microsoft.Build.Logging.BinaryLogger()
			{
				Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic,
				CollectProjectImports = Microsoft.Build.Logging.BinaryLogger.ProjectImportsCollectionMode.None,
				Parameters = $"logfile={outputFilePath}"
			};

			_msbuildLogger.Initialize(_source);
			_source.RaiseBuildStart();

			LogExtensionPoint.AmbientLoggerFactory.AddProvider(this);
		}

		public ILogger CreateLogger(string categoryName)
			=> new BinaryLoggerForwarder(categoryName, _source);

		public void Dispose()
		{
			_source.RaiseBuildFinished();
			_msbuildLogger.Shutdown();
		}

	}
}
