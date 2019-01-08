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
	/// Represents a request from the client. A request is as follows.
	/// 
	///  Field Name         Type                Size (bytes)
	/// ----------------------------------------------------
	///  Length             Integer             4
	///  ProtocolVersion    Integer             4
	///  Language           RequestLanguage     4
	///  CompilerHash       String              Variable
	///  Argument Count     UInteger            4
	///  Arguments          Argument[]          Variable
	/// 
	/// See <see cref="Argument"/> for the format of an
	/// Argument.
	/// 
	/// </summary>
	internal class GenerationRequest
	{
		public readonly uint ProtocolVersion;
		public readonly ReadOnlyCollection<Argument> Arguments;
		public readonly string CompilerHash;

		public GenerationRequest(uint protocolVersion,
							string compilerHash,
							IEnumerable<Argument> arguments)
		{
			ProtocolVersion = protocolVersion;
			Arguments = new ReadOnlyCollection<Argument>(arguments.ToList());
			CompilerHash = compilerHash;

			Debug.Assert(!string.IsNullOrWhiteSpace(CompilerHash), "A hash value is required to communicate with the server");

			if (Arguments.Count > ushort.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(arguments),
					"Too many arguments: maximum of "
					+ ushort.MaxValue + " arguments allowed.");
			}
		}

		public static GenerationRequest Create(string workingDirectory,
										  string tempDirectory,
										  string compilerHash,
										  IList<string> args,
										  string keepAlive = null)
		{
			Debug.Assert(!string.IsNullOrWhiteSpace(compilerHash), "CompilerHash is required to send request to the build server");

			typeof(GenerationRequest).Log().Debug("Creating GenerationRequest");
			typeof(GenerationRequest).Log().Debug($"Working directory: {workingDirectory}");
			typeof(GenerationRequest).Log().Debug($"Temp directory: {tempDirectory}");
			typeof(GenerationRequest).Log().Debug($"Compiler hash: {compilerHash}");

			var requestLength = args.Count + 1;
			var requestArgs = new List<Argument>(requestLength);


			requestArgs.Add(new Argument(ArgumentId.CurrentDirectory, 0, workingDirectory));
			requestArgs.Add(new Argument(ArgumentId.TempDirectory, 0, tempDirectory));

			if (keepAlive != null)
			{
				requestArgs.Add(new Argument(ArgumentId.KeepAlive, 0, keepAlive));
			}

			for (int i = 0; i < args.Count; ++i)
			{
				var arg = args[i];
				typeof(GenerationRequest).Log().Debug($"argument[{i}] = {arg}");
				requestArgs.Add(new Argument(ArgumentId.CommandLineArgument, i, arg));
			}

			return new GenerationRequest(GenerationProtocolConstants.ProtocolVersion, compilerHash, requestArgs);
		}

		public static GenerationRequest CreateShutdown()
		{
			var requestArgs = new[] { new Argument(ArgumentId.Shutdown, argumentIndex: 0, value: "") };
			return new GenerationRequest(GenerationProtocolConstants.ProtocolVersion, GetCommitHash(), requestArgs);
		}

		/// <summary>
		/// Read a Request from the given stream.
		/// 
		/// The total request size must be less than 1MB.
		/// </summary>
		/// <returns>null if the Request was too large, the Request otherwise.</returns>
		public static async Task<GenerationRequest> ReadAsync(Stream inStream, CancellationToken cancellationToken)
		{
			// Read the length of the request
			var lengthBuffer = new byte[4];
			typeof(GenerationRequest).Log().Debug("Reading length of request");
			await ReadAllAsync(inStream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
			var length = BitConverter.ToInt32(lengthBuffer, 0);

			// Back out if the request is > 1MB
			if (length > 0x100000)
			{
				typeof(GenerationRequest).Log().Debug("Request is over 1MB in length, cancelling read.");
				return null;
			}

			cancellationToken.ThrowIfCancellationRequested();

			// Read the full request
			var requestBuffer = new byte[length];
			await ReadAllAsync(inStream, requestBuffer, length, cancellationToken).ConfigureAwait(false);

			cancellationToken.ThrowIfCancellationRequested();

			typeof(GenerationRequest).Log().Debug("Parsing request");
			// Parse the request into the Request data structure.
			using (var reader = new BinaryReader(new MemoryStream(requestBuffer), Encoding.Unicode))
			{
				var protocolVersion = reader.ReadUInt32();
				var compilerHash = reader.ReadString();
				uint argumentCount = reader.ReadUInt32();

				var argumentsBuilder = new List<Argument>((int)argumentCount);

				for (int i = 0; i < argumentCount; i++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					argumentsBuilder.Add(GenerationRequest.Argument.ReadFromBinaryReader(reader));
				}

				return new GenerationRequest(protocolVersion,
										compilerHash,
										argumentsBuilder);
			}
		}

		/// <summary>
		/// Write a Request to the stream.
		/// </summary>
		public async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var memoryStream = new MemoryStream())
			using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
			{
				// Format the request.
				typeof(GenerationRequest).Log().Debug("Formatting request");
				writer.Write(ProtocolVersion);
				writer.Write(CompilerHash);
				writer.Write(Arguments.Count);
				foreach (Argument arg in Arguments)
				{
					cancellationToken.ThrowIfCancellationRequested();
					arg.WriteToBinaryWriter(writer);
				}
				writer.Flush();

				cancellationToken.ThrowIfCancellationRequested();

				// Write the length of the request
				int length = checked((int)memoryStream.Length);

				// Back out if the request is > 1 MB
				if (memoryStream.Length > 0x100000)
				{
					typeof(GenerationRequest).Log().Debug("Request is over 1MB in length, cancelling write");
					throw new ArgumentOutOfRangeException();
				}

				// Send the request to the server
				typeof(GenerationRequest).Log().Debug("Writing length of request.");
				await outStream.WriteAsync(BitConverter.GetBytes(length), 0, 4,
										   cancellationToken).ConfigureAwait(false);

				typeof(GenerationRequest).Log().Debug($"Writing request of size {length}");
				// Write the request
				memoryStream.Position = 0;
				await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// A command line argument to the compilation. 
		/// An argument is formatted as follows:
		/// 
		///  Field Name         Type            Size (bytes)
		/// --------------------------------------------------
		///  ID                 UInteger        4
		///  Index              UInteger        4
		///  Value              String          Variable
		/// 
		/// Strings are encoded via a length prefix as a signed
		/// 32-bit integer, followed by an array of characters.
		/// </summary>
		public struct Argument
		{
			public readonly ArgumentId ArgumentId;
			public readonly int ArgumentIndex;
			public readonly string Value;

			public Argument(ArgumentId argumentId,
							int argumentIndex,
							string value)
			{
				ArgumentId = argumentId;
				ArgumentIndex = argumentIndex;
				Value = value;
			}

			public static Argument ReadFromBinaryReader(BinaryReader reader)
			{
				var argId = (ArgumentId)reader.ReadInt32();
				var argIndex = reader.ReadInt32();
				string value = ReadLengthPrefixedString(reader);
				return new Argument(argId, argIndex, value);
			}

			public void WriteToBinaryWriter(BinaryWriter writer)
			{
				writer.Write((int)ArgumentId);
				writer.Write(ArgumentIndex);
				WriteLengthPrefixedString(writer, Value);
			}
		}
	}
}
