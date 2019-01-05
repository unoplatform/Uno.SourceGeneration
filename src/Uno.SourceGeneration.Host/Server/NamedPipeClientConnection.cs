// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host.GenerationClient;
using Uno.SourceGeneration.Host.Helpers;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Server
{
	internal sealed class NamedPipeClientConnection : ClientConnection
    {
        private readonly NamedPipeServerStream _pipeStream;

        internal NamedPipeClientConnection(IGenerationServerHost compilerServerHost, string loggingIdentifier, NamedPipeServerStream pipeStream)
            : base(compilerServerHost, loggingIdentifier, pipeStream)
        {
            _pipeStream = pipeStream;
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection. 
        ///
        /// This will return true if the pipe was disconnected.
        /// </summary>
        protected override Task CreateMonitorDisconnectTask(CancellationToken cancellationToken)
        {
            return GenerationServerConnection.CreateMonitorDisconnectTask(_pipeStream, LoggingIdentifier, cancellationToken);
        }

        protected override void ValidateGenerationRequest(GenerationRequest request)
        {
            // Now that we've read data from the stream we can validate the identity.
            if (!ClientAndOurIdentitiesMatch(_pipeStream))
            {
                throw new Exception("Client identity does not match server identity.");
            }
        }

        /// <summary>
        /// Does the client of "pipeStream" have the same identity and elevation as we do?
        /// </summary>
        private bool ClientAndOurIdentitiesMatch(NamedPipeServerStream pipeStream)
        {
            if (PlatformInformation.IsWindows)
            {
                var serverIdentity = GetIdentity(impersonating: false);

                (string name, bool admin) clientIdentity = default;
                pipeStream.RunAsClient(() => { clientIdentity = GetIdentity(impersonating: true); });

                this.Log().Debug($"Server identity = '{serverIdentity.name}', server elevation='{serverIdentity.admin}'.");
                this.Log().Debug($"Client identity = '{clientIdentity.name}', client elevation='{serverIdentity.admin}'.");

                return
                    StringComparer.OrdinalIgnoreCase.Equals(serverIdentity.name, clientIdentity.name) &&
                    serverIdentity.admin == clientIdentity.admin;
            }
            else
            {
                return GenerationServerConnection.CheckIdentityUnix(pipeStream);
            }
        }

        /// <summary>
        /// Return the current user name and whether the current user is in the administrator role.
        /// </summary>
        private static (string name, bool admin) GetIdentity(bool impersonating)
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent(impersonating);
            WindowsPrincipal currentPrincipal = new WindowsPrincipal(currentIdentity);
            var elevatedToAdmin = currentPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
            return (currentIdentity.Name, elevatedToAdmin);
        }

        public override void Close()
        {
            this.Log().Debug($"Pipe {LoggingIdentifier}: Closing.");
            try
            {
                _pipeStream.Close();
            }
            catch (Exception e)
            {
                // The client connection failing to close isn't fatal to the server process.  It is simply a client
                // for which we can no longer communicate and that's okay because the Close method indicates we are
                // done with the client already.
                var msg = string.Format($"Pipe {LoggingIdentifier}: Error closing pipe.");
                this.Log().Error(msg, e);
            }
        }
    }
}
