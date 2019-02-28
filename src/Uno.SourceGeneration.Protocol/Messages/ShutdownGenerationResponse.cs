// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System.IO;

namespace Uno.SourceGeneration.Host.Messages
{
	internal sealed class ShutdownGenerationResponse : GenerationResponse
	{
		public readonly int ServerProcessId;

		public ShutdownGenerationResponse(int serverProcessId)
		{
			ServerProcessId = serverProcessId;
		}

		public override ResponseType Type => ResponseType.Shutdown;

		protected override void AddResponseBody(BinaryWriter writer)
		{
			writer.Write(ServerProcessId);
		}

		public static ShutdownGenerationResponse Create(BinaryReader reader)
		{
			var serverProcessId = reader.ReadInt32();
			return new ShutdownGenerationResponse(serverProcessId);
		}
	}
}
