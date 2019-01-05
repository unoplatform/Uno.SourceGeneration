// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using Uno.SourceGeneration.Helpers;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Server
{
	internal abstract partial class GenerationServerHost : IGenerationServerHost
	{
		public abstract Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider { get; }

        /// <summary>
        /// Directory that contains the compiler executables and the response files. 
        /// </summary>
        public string ClientDirectory { get; }

        protected GenerationServerHost(string clientDirectory)
        {
            ClientDirectory = clientDirectory;
        }

		public GenerationResponse RunGeneration(RunRequest request, CancellationToken cancellationToken)
		{
			Log($"CurrentDirectory = '{request.CurrentDirectory}'");
			for (int i = 0; i < request.Arguments.Length; ++i)
			{
				Log($"Argument[{i}] = '{request.Arguments[i]}'");
			}

			// Compiler server must be provided with a valid temporary directory in order to correctly
			// isolate signing between compilations.
			if (string.IsNullOrEmpty(request.TempDirectory))
			{
				Log($"Rejecting build due to missing temp directory");
				return new RejectedGenerationResponse();
			}

			var responseFilePath = request.Arguments[0];
			var generatedFilesOutputPath = request.Arguments[1];
			var binlogOutputPath = request.Arguments[2];
			var enableConsole = request.Arguments.ElementAtOrDefault(3)?.Equals("-console", StringComparison.OrdinalIgnoreCase) ?? false;

			using (var responseFile = File.OpenRead(request.Arguments[0]))
			{
				var env = new DataContractSerializer(typeof(BuildEnvironment));

				if (env.ReadObject(responseFile) is BuildEnvironment environment)
				{
					AssemblyResolver.RegisterAssemblyLoader(environment);

					using (var logger = new BinaryLoggerForwarderProvider(binlogOutputPath))
					{
						typeof(Program).Log().Info($"Generating files to path {generatedFilesOutputPath}, logoutput={binlogOutputPath}");

						try
						{
							string[] generatedFiles = Generate(environment);

							File.WriteAllText(generatedFilesOutputPath, string.Join(";", generatedFiles));

							return new CompletedGenerationResponse(0, true, "empty output");
						}
						catch (Exception e)
						{
							if (enableConsole)
							{
								Console.WriteLine(e.ToString());
							}

							typeof(Program).Log().Error("Generation failed: " + e.ToString());

							return new RejectedGenerationResponse();
						}
					}
				}
				else
				{
					return new RejectedGenerationResponse();
				}
			}
		}

		private void Log(string message) => this.Log().Debug(message);
	}
}
