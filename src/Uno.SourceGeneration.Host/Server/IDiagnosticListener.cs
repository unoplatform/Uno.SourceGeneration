// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;

namespace Uno.SourceGeneration.Host.Server
{
	internal interface IDiagnosticListener
    {
        /// <summary>
        /// Called when the server updates the keep alive value.
        /// </summary>
        void UpdateKeepAlive(TimeSpan timeSpan);

        /// <summary>
        /// Called each time the server listens for new connections.
        /// </summary>
        void ConnectionListening();

        /// <summary>
        /// Called when a connection to the server occurs.
        /// </summary>
        void ConnectionReceived();

        /// <summary>
        /// Called when one or more connections have completed processing.  The number of connections
        /// processed is provided in <paramref name="count"/>.
        /// </summary>
        void ConnectionCompleted(int count);

        /// <summary>
        /// Called when a bad client connection was detected and the server will be shutting down as a 
        /// result.
        /// </summary>
        void ConnectionRudelyEnded();

        /// <summary>
        /// Called when the server is shutting down because the keep alive timeout was reached.
        /// </summary>
        void KeepAliveReached();
    }
}
