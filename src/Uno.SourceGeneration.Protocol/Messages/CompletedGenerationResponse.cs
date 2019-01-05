// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.IO;
using static Uno.SourceGeneration.Host.Messages.GenerationProtocolConstants;

namespace Uno.SourceGeneration.Host.Messages
{
	/// <summary>
	/// Represents a Response from the server. A response is as follows.
	/// 
	///  Field Name         Type            Size (bytes)
	/// --------------------------------------------------
	///  Length             UInteger        4
	///  ReturnCode         Integer         4
	///  Output             String          Variable
	///  ErrorOutput        String          Variable
	/// 
	/// Strings are encoded via a character count prefix as a 
	/// 32-bit integer, followed by an array of characters.
	/// 
	/// </summary>
	internal sealed class CompletedGenerationResponse : GenerationResponse
	{
		public readonly int ReturnCode;
		public readonly bool Utf8Output;
		public readonly string Output;
		public readonly string ErrorOutput;

		public CompletedGenerationResponse(int returnCode,
									  bool utf8output,
									  string output)
		{
			ReturnCode = returnCode;
			Utf8Output = utf8output;
			Output = output;

			// This field existed to support writing to Console.Error.  The compiler doesn't ever write to 
			// this field or Console.Error.  This field is only kept around in order to maintain the existing
			// protocol semantics.
			ErrorOutput = string.Empty;
		}

		public override ResponseType Type => ResponseType.Completed;

		public static CompletedGenerationResponse Create(BinaryReader reader)
		{
			var returnCode = reader.ReadInt32();
			var utf8Output = reader.ReadBoolean();
			var output = ReadLengthPrefixedString(reader);
			var errorOutput = ReadLengthPrefixedString(reader);
			if (!string.IsNullOrEmpty(errorOutput))
			{
				throw new InvalidOperationException();
			}

			return new CompletedGenerationResponse(returnCode, utf8Output, output);
		}

		protected override void AddResponseBody(BinaryWriter writer)
		{
			writer.Write(ReturnCode);
			writer.Write(Utf8Output);
			WriteLengthPrefixedString(writer, Output);
			WriteLengthPrefixedString(writer, ErrorOutput);
		}
	}
}
