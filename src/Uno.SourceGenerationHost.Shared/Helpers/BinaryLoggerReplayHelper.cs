using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;

namespace Uno.SourceGeneration.Helpers
{
	internal class BinaryLoggerReplayHelper
	{
		/// <summary>
		/// Replays the provided binlog file in the current build engine
		/// </summary>
		public static void Replay(IBuildEngine engine, string filePath)
		{
			var replaySource = new Microsoft.Build.Logging.BinaryLogReplayEventSource();

			replaySource.MessageRaised += (s, e) => engine.LogMessageEvent(e);

			replaySource.Replay(filePath);
		}
	}
}
