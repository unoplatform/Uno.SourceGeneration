// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.SourceGeneration.Host.Messages;

namespace Uno.SourceGeneration.Host.Server
{
	internal static class GenerationProtocolUtil
	{
		internal static RunRequest GetRunRequest(GenerationRequest req)
		{
			string currentDirectory;
			string libDirectory;
			string tempDirectory;
			string[] arguments = GetCommandLineArguments(req, out currentDirectory, out tempDirectory, out libDirectory);

			return new RunRequest(currentDirectory, tempDirectory, arguments);
		}

		internal static string[] GetCommandLineArguments(GenerationRequest req, out string currentDirectory, out string tempDirectory, out string libDirectory)
		{
			currentDirectory = null;
			libDirectory = null;
			tempDirectory = null;
			List<string> commandLineArguments = new List<string>();

			foreach (GenerationRequest.Argument arg in req.Arguments)
			{
				if (arg.ArgumentId == GenerationProtocolConstants.ArgumentId.CurrentDirectory)
				{
					currentDirectory = arg.Value;
				}
				else if (arg.ArgumentId == GenerationProtocolConstants.ArgumentId.TempDirectory)
				{
					tempDirectory = arg.Value;
				}
				else if (arg.ArgumentId == GenerationProtocolConstants.ArgumentId.CommandLineArgument)
				{
					int argIndex = arg.ArgumentIndex;
					while (argIndex >= commandLineArguments.Count)
					{
						commandLineArguments.Add("");
					}

					commandLineArguments[argIndex] = arg.Value;
				}
			}

			return commandLineArguments.ToArray();
		}
	}
}
