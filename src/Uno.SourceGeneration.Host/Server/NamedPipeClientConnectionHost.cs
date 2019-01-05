// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using Uno.SourceGeneratorTasks.Helpers;
using Uno.SourceGeneration.Host.Helpers;

namespace Uno.SourceGeneration.Host.Server
{
	internal sealed class NamedPipeClientConnectionHost : IClientConnectionHost
    {
        // Size of the buffers to use: 64K
        private const int PipeBufferSize = 0x10000;

        private readonly IGenerationServerHost _compilerServerHost;
        private readonly string _pipeName;
        private int _loggingIdentifier;

        internal NamedPipeClientConnectionHost(IGenerationServerHost compilerServerHost, string pipeName)
        {
            _compilerServerHost = compilerServerHost;
            _pipeName = pipeName;
        }

        public async Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken)
        {
            var pipeStream = await CreateListenTaskCore(cancellationToken).ConfigureAwait(false);
            return new NamedPipeClientConnection(_compilerServerHost, _loggingIdentifier++.ToString(), pipeStream);
        }

        /// <summary>
        /// Creates a Task that waits for a client connection to occur and returns the connected 
        /// <see cref="NamedPipeServerStream"/> object.  Throws on any connection error.
        /// </summary>
        /// <param name="cancellationToken">Used to cancel the connection sequence.</param>
        private async Task<NamedPipeServerStream> CreateListenTaskCore(CancellationToken cancellationToken)
        {
            // Create the pipe and begin waiting for a connection. This 
            // doesn't block, but could fail in certain circumstances, such
            // as Windows refusing to create the pipe for some reason 
            // (out of handles?), or the pipe was disconnected before we 
            // starting listening.
            NamedPipeServerStream pipeStream = ConstructPipe(_pipeName);

            this.Log().Debug("Waiting for new connection");
            await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            this.Log().Debug("Pipe connection detected.");

            if (Environment.Is64BitProcess || MemoryHelper.IsMemoryAvailable())
            {
                this.Log().Debug("Memory available - accepting connection");
                return pipeStream;
            }

            pipeStream.Close();
            throw new Exception("Insufficient resources to process new connection.");
        }

        /// <summary>
        /// Create an instance of the pipe. This might be the first instance, or a subsequent instance.
        /// There always needs to be an instance of the pipe created to listen for a new client connection.
        /// </summary>
        /// <returns>The pipe instance or throws an exception.</returns>
        private NamedPipeServerStream ConstructPipe(string pipeName)
        {
            this.Log().Debug($"Constructing pipe '{pipeName}'.");

#if NETFRAMEWORK
            PipeSecurity security;
            PipeOptions pipeOptions = PipeOptions.Asynchronous | PipeOptions.WriteThrough;

            if (!PlatformInformation.IsRunningOnMono)
            {
                security = new PipeSecurity();
                SecurityIdentifier identifier = WindowsIdentity.GetCurrent().Owner;

                // Restrict access to just this account.  
                PipeAccessRule rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
                security.AddAccessRule(rule);
                security.SetOwner(identifier);
            }
            else
            {
                // Pipe security and additional access rights constructor arguments
                //  are not supported by Mono 
                // https://github.com/dotnet/roslyn/pull/30810
                // https://github.com/mono/mono/issues/11406
                security = null;
                // This enum value is implemented by Mono to restrict pipe access to
                //  the current user
                const int CurrentUserOnly = unchecked((int)0x20000000);
                pipeOptions |= (PipeOptions)CurrentUserOnly;
            }

            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                pipeOptions,
                PipeBufferSize, // Default input buffer
                PipeBufferSize, // Default output buffer
                security,
                HandleInheritability.None);
#else
            // The overload of NamedPipeServerStream with the PipeAccessRule
            // parameter was removed in netstandard. However, the default
            // constructor does not provide WRITE_DAC, so attempting to use
            // SetAccessControl will always fail. So, completely ignore ACLs on
            // netcore, and trust that our `ClientAndOurIdentitiesMatch`
            // verification will catch any invalid connections.
            // Issue to add WRITE_DAC support:
            // https://github.com/dotnet/corefx/issues/24040
            NamedPipeServerStream pipeStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                PipeBufferSize, // Default input buffer
                PipeBufferSize);// Default output buffer
#endif

            this.Log().Debug($"Successfully constructed pipe '{pipeName}'.");

            return pipeStream;
        }
    }
}
