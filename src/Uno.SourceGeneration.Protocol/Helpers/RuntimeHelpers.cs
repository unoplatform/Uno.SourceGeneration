using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.SourceGeneration.Helpers
{
	public class RuntimeHelpers
	{
		public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(".NET Core");

		public static bool IsMono => Type.GetType("Mono.Runtime") != null;
	}
}
