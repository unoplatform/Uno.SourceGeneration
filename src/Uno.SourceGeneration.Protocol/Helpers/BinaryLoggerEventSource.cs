using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;

namespace Uno.SourceGeneration.Helpers
{
	public class BinaryLoggerEventSource : IEventSource
	{
#pragma warning disable CS0067
		public event BuildMessageEventHandler MessageRaised;
		public event BuildErrorEventHandler ErrorRaised;
		public event BuildWarningEventHandler WarningRaised;
		public event BuildStartedEventHandler BuildStarted;
		public event BuildFinishedEventHandler BuildFinished;
		public event ProjectStartedEventHandler ProjectStarted;
		public event ProjectFinishedEventHandler ProjectFinished;
		public event TargetStartedEventHandler TargetStarted;
		public event TargetFinishedEventHandler TargetFinished;
		public event TaskStartedEventHandler TaskStarted;
		public event TaskFinishedEventHandler TaskFinished;
		public event CustomBuildEventHandler CustomEventRaised;
		public event BuildStatusEventHandler StatusEventRaised;
#pragma warning restore CS0067

		public event AnyEventHandler AnyEventRaised;

		public void RaiseMessage(string senderName, string message, MessageImportance importance)
			=> AnyEventRaised?.Invoke(this, new BuildMessageEventArgs(message, "", senderName, importance));

		public void RaiseWarning(string senderName, string message)
			=> AnyEventRaised?.Invoke(this, new BuildWarningEventArgs("", "", "", 0, 0, 0, 0, message, "", senderName));

		public void RaiseError(string senderName, string message)
			=> AnyEventRaised?.Invoke(this, new BuildErrorEventArgs("", "", "", 0, 0, 0, 0, message, "", senderName));

		public void RaiseBuildStart()
			=> AnyEventRaised?.Invoke(this, new BuildStartedEventArgs("Build Started", "", DateTime.Now));

		public void RaiseBuildFinished()
			=> AnyEventRaised?.Invoke(this, new BuildFinishedEventArgs("Build Started", "", true));
	}

}
