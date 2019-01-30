// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

namespace Uno.SourceGeneration.Host.Server
{
	internal enum CompletionReason
    {
        /// <summary>
        /// There was an error creating the <see cref="GenerationRequest"/> object and a compilation was never
        /// created.
        /// </summary>
        CompilationNotStarted,

        /// <summary>
        /// The compilation completed and results were provided to the client.
        /// </summary>
        CompilationCompleted,

        /// <summary>
        /// The compilation process was initiated and the client disconnected before
        /// the results could be provided to them.
        /// </summary>
        ClientDisconnect,

        /// <summary>
        /// There was an unhandled exception processing the result.
        /// </summary>
        ClientException,

        /// <summary>
        /// There was a request from the client to shutdown the server.
        /// </summary>
        ClientShutdownRequest,
    }
}
