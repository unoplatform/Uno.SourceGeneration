// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System.Threading;
using System.Threading.Tasks;

namespace Uno.SourceGeneration.Host.Server
{
    /// <summary>
    /// Abstraction over the connection to the client process.   This hides underlying connection
    /// to facilitate better testing. 
    /// </summary>
    internal interface IClientConnection
    {
        /// <summary>
        /// A value which can be used to identify this connection for logging purposes only.  It has 
        /// no guarantee of uniqueness.  
        /// </summary>
        string LoggingIdentifier { get; }

        /// <summary>
        /// Server the connection and return the result.
        /// </summary>
        Task<ConnectionData> HandleConnection(bool allowCompilationRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Close the underlying client connection.
        /// </summary>
        void Close();
    }
}
