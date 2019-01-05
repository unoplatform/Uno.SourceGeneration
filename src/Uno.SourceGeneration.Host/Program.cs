using System.Diagnostics;
using Uno.SourceGeneration.Host.Helpers;
using Uno.SourceGeneration.Host.Server;

[assembly: CommitHashAttribute("<developer build>")]

namespace Uno.SourceGeneration.Host
{
	class Program
	{
		static int Main(string[] args)
		{
			// Debugger.Launch();
			System.Console.WriteLine("Starting host");
			return new DesktopGenerationServerController(new System.Collections.Specialized.NameValueCollection()).Run(args);
		}
	}
}
