using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.SourceGeneration.Helpers
{
	internal class BinaryLoggerReplayHelper
	{
		/// <summary>
		/// Replays the provided binlog file in the current build engine
		/// </summary>
		public static void Replay(IBuildEngine engine, string filePath, TaskLoggingHelper log)
		{
			if (File.Exists(filePath))
			{
				try
				{
					var replaySource = new Microsoft.Build.Logging.BinaryLogReplayEventSource();

					replaySource.MessageRaised += (s, e) => engine.LogMessageEvent(e);
					replaySource.WarningRaised += (s, e) => engine.LogWarningEvent(e);
					replaySource.ErrorRaised += (s, e) => engine.LogErrorEvent(e);

					replaySource.Replay(filePath);
				}
				catch(Exception e)
				{
					var fileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : -1;

					log.LogWarning($"Failed to replay source generation controller build messages (path:{filePath}, size:{fileSize}) : {e}");
				}
			}
		}
	}
}
