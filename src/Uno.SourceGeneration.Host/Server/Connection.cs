// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Server
{

	/// <summary>
	/// Represents a single connection from a client process. Handles the named pipe
	/// from when the client connects to it, until the request is finished or abandoned.
	/// A new task is created to actually service the connection and do the operation.
	/// </summary>
	internal abstract class ClientConnection : IClientConnection
    {
        private readonly IGenerationServerHost _generationServerHost;
        private readonly string _loggingIdentifier;
        private readonly Stream _stream;

        public string LoggingIdentifier => _loggingIdentifier;

        public ClientConnection(IGenerationServerHost compilerServerHost, string loggingIdentifier, Stream stream)
        {
            _generationServerHost = compilerServerHost;
            _loggingIdentifier = loggingIdentifier;
            _stream = stream;
        }

        /// <summary>
        /// Returns a Task that resolves if the client stream gets disconnected.
        /// </summary>
        protected abstract Task CreateMonitorDisconnectTask(CancellationToken cancellationToken);

        protected virtual void ValidateGenerationRequest(GenerationRequest request)
        {
        }

        /// <summary>
        /// Close the connection.  Can be called multiple times.
        /// </summary>
        public abstract void Close();

        public async Task<ConnectionData> HandleConnection(bool allowCompilationRequests = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                GenerationRequest request;
                try
                {
                    Log("Begin reading request.");
                    request = await GenerationRequest.ReadAsync(_stream, cancellationToken).ConfigureAwait(false);
                    ValidateGenerationRequest(request);
                    Log("End reading request.");
                }
                catch (Exception e)
                {
                    LogException(e, "Error reading build request.");
                    return new ConnectionData(CompletionReason.CompilationNotStarted);
                }

                if (request.ProtocolVersion != GenerationProtocolConstants.ProtocolVersion)
                {
                    return await HandleMismatchedVersionRequest(cancellationToken).ConfigureAwait(false);
                }
                else if (!string.Equals(request.CompilerHash, GenerationProtocolConstants.GetCommitHash(), StringComparison.OrdinalIgnoreCase))
                {
                    return await HandleIncorrectHashRequest(cancellationToken).ConfigureAwait(false);
                }
                else if (IsShutdownRequest(request))
                {
                    return await HandleShutdownRequest(cancellationToken).ConfigureAwait(false);
                }
                else if (!allowCompilationRequests)
                {
                    return await HandleRejectedRequest(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await HandleCompilationRequest(request, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Close();
            }
        }

        private async Task<ConnectionData> HandleCompilationRequest(GenerationRequest request, CancellationToken cancellationToken)
        {
            var keepAlive = CheckForNewKeepAlive(request);

            // Kick off both the compilation and a task to monitor the pipe for closing.
            var buildCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var compilationTask = ServeGenerationRequest(request, buildCts.Token);
            var monitorTask = CreateMonitorDisconnectTask(buildCts.Token);
            await Task.WhenAny(compilationTask, monitorTask).ConfigureAwait(false);

            // Do an 'await' on the completed task, preference being compilation, to force
            // any exceptions to be realized in this method for logging.
            CompletionReason reason;
            if (compilationTask.IsCompleted)
            {
                var response = await compilationTask.ConfigureAwait(false);

                try
                {
                    Log("Begin writing response.");
                    await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
                    reason = CompletionReason.CompilationCompleted;
                    Log("End writing response.");
                }
                catch
                {
                    reason = CompletionReason.ClientDisconnect;
                }
            }
            else
            {
                await monitorTask.ConfigureAwait(false);
                reason = CompletionReason.ClientDisconnect;
            }

            // Begin the tear down of the Task which didn't complete.
            buildCts.Cancel();
            return new ConnectionData(reason, keepAlive);
        }

        private async Task<ConnectionData> HandleMismatchedVersionRequest(CancellationToken cancellationToken)
        {
            var response = new MismatchedVersionGenerationResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleIncorrectHashRequest(CancellationToken cancellationToken)
        {
            var response = new IncorrectHashGenerationResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleRejectedRequest(CancellationToken cancellationToken)
        {
            var response = new RejectedGenerationResponse();
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.CompilationNotStarted);
        }

        private async Task<ConnectionData> HandleShutdownRequest(CancellationToken cancellationToken)
        {
            var id = Process.GetCurrentProcess().Id;
            var response = new ShutdownGenerationResponse(id);
            await response.WriteAsync(_stream, cancellationToken).ConfigureAwait(false);
            return new ConnectionData(CompletionReason.ClientShutdownRequest);
        }

        /// <summary>
        /// Check the request arguments for a new keep alive time. If one is present,
        /// set the server timer to the new time.
        /// </summary>
        private TimeSpan? CheckForNewKeepAlive(GenerationRequest request)
        {
            TimeSpan? timeout = null;
            foreach (var arg in request.Arguments)
            {
                if (arg.ArgumentId == GenerationProtocolConstants.ArgumentId.KeepAlive)
                {
                    int result;
                    // If the value is not a valid integer for any reason,
                    // ignore it and continue with the current timeout. The client
                    // is responsible for validating the argument.
                    if (int.TryParse(arg.Value, out result))
                    {
                        // Keep alive times are specified in seconds
                        timeout = TimeSpan.FromSeconds(result);
                    }
                }
            }

            return timeout;
        }

        private bool IsShutdownRequest(GenerationRequest request)
        {
            return request.Arguments.Count == 1 && request.Arguments[0].ArgumentId == GenerationProtocolConstants.ArgumentId.Shutdown;
        }

        protected virtual Task<GenerationResponse> ServeGenerationRequest(GenerationRequest generationRequest, CancellationToken cancellationToken)
        {
            Func<GenerationResponse> func = () =>
            {
                // Do the compilation
                Log("Begin compilation");

                var request = GenerationProtocolUtil.GetRunRequest(generationRequest);
                var response = _generationServerHost.RunGeneration(request, cancellationToken);

                Log("End compilation");
                return response;
            };

            var task = new Task<GenerationResponse>(func, cancellationToken, TaskCreationOptions.LongRunning);
            task.Start();
            return task;
        }

        private void Log(string message)
        {
            this.Log().DebugFormat("Client {0}: {1}", _loggingIdentifier, message);
        }

        private void LogException(Exception e, string message)
        {
			this.Log().Error(string.Format("Client {0}: {1}", _loggingIdentifier, message), e);
        }
    }
}
