// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.SourceGeneratorTasks.Helpers;
using static Uno.SourceGeneration.Host.Messages.GenerationProtocolConstants;

namespace Uno.SourceGeneration.Host.Messages
{
	/// <summary>
	/// Base class for all possible responses to a request.
	/// The ResponseType enum should list all possible response types
	/// and ReadResponse creates the appropriate response subclass based
	/// on the response type sent by the client.
	/// The format of a response is:
	///
	/// Field Name       Field Type          Size (bytes)
	/// -------------------------------------------------
	/// responseLength   int (positive)      4  
	/// responseType     enum ResponseType   4
	/// responseBody     Response subclass   variable
	/// </summary>
	internal abstract class GenerationResponse
	{
		public enum ResponseType
		{
			// The client and server are using incompatible protocol versions.
			MismatchedVersion,

			// The build request completed on the server and the results are contained
			// in the message. 
			Completed,

			// The build request could not be run on the server due because it created
			// an unresolvable inconsistency with analyzers.  
			AnalyzerInconsistency,

			// The shutdown request completed and the server process information is 
			// contained in the message. 
			Shutdown,

			// The request was rejected by the server.  
			Rejected,

			// The server hash did not match the one supplied by the client
			IncorrectHash,
		}

		public abstract ResponseType Type { get; }

		public async Task WriteAsync(Stream outStream,
							   CancellationToken cancellationToken)
		{
			using (var memoryStream = new MemoryStream())
			using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
			{
				// Format the response
				this.Log().Debug("Formatting Response");
				writer.Write((int)Type);

				AddResponseBody(writer);
				writer.Flush();

				cancellationToken.ThrowIfCancellationRequested();

				// Send the response to the client

				// Write the length of the response
				int length = checked((int)memoryStream.Length);

				this.Log().Debug("Writing response length");
				// There is no way to know the number of bytes written to
				// the pipe stream. We just have to assume all of them are written.
				await outStream.WriteAsync(BitConverter.GetBytes(length),
										   0,
										   4,
										   cancellationToken).ConfigureAwait(false);

				// Write the response
				this.Log().Debug($"Writing response of size {length}");
				memoryStream.Position = 0;
				await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
		}

		protected abstract void AddResponseBody(BinaryWriter writer);

		/// <summary>
		/// May throw exceptions if there are pipe problems.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<GenerationResponse> ReadAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
		{
			typeof(GenerationResponse).Log().Debug("Reading response length");
			// Read the response length
			var lengthBuffer = new byte[4];
			await ReadAllAsync(stream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
			var length = BitConverter.ToUInt32(lengthBuffer, 0);

			// Read the response
			typeof(GenerationResponse).Log().Debug($"Reading response of length {length}");
			var responseBuffer = new byte[length];
			await ReadAllAsync(stream,
							   responseBuffer,
							   responseBuffer.Length,
							   cancellationToken).ConfigureAwait(false);

			using (var reader = new BinaryReader(new MemoryStream(responseBuffer), Encoding.Unicode))
			{
				var responseType = (ResponseType)reader.ReadInt32();

				switch (responseType)
				{
					case ResponseType.Completed:
						return CompletedGenerationResponse.Create(reader);
					//case ResponseType.MismatchedVersion:
					//	return new MismatchedVersionBuildResponse();
					//case ResponseType.IncorrectHash:
					//	return new IncorrectHashBuildResponse();
					//case ResponseType.AnalyzerInconsistency:
					//	return new AnalyzerInconsistencyBuildResponse();
					case ResponseType.Shutdown:
						return ShutdownGenerationResponse.Create(reader);
					//case ResponseType.Rejected:
					//	return new RejectedBuildResponse();
					default:
						throw new InvalidOperationException("Received invalid response type from server.");
				}
			}
		}
	}
}
