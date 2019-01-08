// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host.Helpers;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Messages
{
	/// <summary>
	/// Constants about the protocol.
	/// </summary>
	internal static class GenerationProtocolConstants
	{
		/// <summary>
		/// The version number for this protocol.
		/// </summary>
		public const uint ProtocolVersion = 3;

		// Arguments for CSharp and VB Compiler
		public enum ArgumentId
		{
			// The current directory of the client
			CurrentDirectory = 0x51147221,

			// A comment line argument. The argument index indicates which one (0 .. N)
			CommandLineArgument,

			// Request a longer keep alive time for the server
			KeepAlive,

			// Request a server shutdown from the client
			Shutdown,

			// The directory to use for temporary operations.
			TempDirectory,
		}

		/// <summary>
		/// Read a string from the Reader where the string is encoded
		/// as a length prefix (signed 32-bit integer) followed by
		/// a sequence of characters.
		/// </summary>
		public static string ReadLengthPrefixedString(BinaryReader reader)
		{
			var length = reader.ReadInt32();
			return new String(reader.ReadChars(length));
		}

		/// <summary>
		/// Write a string to the Writer where the string is encoded
		/// as a length prefix (signed 32-bit integer) follows by
		/// a sequence of characters.
		/// </summary>
		public static void WriteLengthPrefixedString(BinaryWriter writer, string value)
		{
			writer.Write(value.Length);
			writer.Write(value.ToCharArray());
		}

		/// <summary>
		/// Reads the value of <see cref="CommitHashAttribute.Hash"/> of the assembly <see cref="GenerationRequest"/> is defined in
		/// </summary>
		/// <returns>The hash value of the current assembly or an empty string</returns>
		public static string GetCommitHash()
		{
			var hashAttributes = typeof(GenerationRequest).Assembly.GetCustomAttributes(typeof(CommitHashAttribute), false).OfType<CommitHashAttribute>();
			var hashAttributeCount = hashAttributes.Count();
			if (hashAttributeCount != 1)
			{
				typeof(GenerationProtocolConstants).Log().Error($"Error reading CommitHashAttribute. Exactly 1 attribute is required, found {hashAttributeCount}");
				return string.Empty;
			}
			return hashAttributes.Single().Hash;
		}

		/// <summary>
		/// This task does not complete until we are completely done reading.
		/// </summary>
		internal static async Task ReadAllAsync(
			Stream stream,
			byte[] buffer,
			int count,
			CancellationToken cancellationToken)
		{
			int totalBytesRead = 0;
			do
			{
				typeof(GenerationProtocolConstants).Log().DebugFormat("Attempting to read {0} bytes from the stream",
					count - totalBytesRead);
				int bytesRead = await stream.ReadAsync(buffer,
													   totalBytesRead,
													   count - totalBytesRead,
													   cancellationToken).ConfigureAwait(false);
				if (bytesRead == 0)
				{
					typeof(GenerationProtocolConstants).Log().Debug("Unexpected -- read 0 bytes from the stream.");
					throw new EndOfStreamException("Reached end of stream before end of read.");
				}
				typeof(GenerationProtocolConstants).Log().DebugFormat("Read {0} bytes", bytesRead);
				totalBytesRead += bytesRead;
			} while (totalBytesRead < count);
			typeof(GenerationProtocolConstants).Log().Debug("Finished read");
		}
	}

}
