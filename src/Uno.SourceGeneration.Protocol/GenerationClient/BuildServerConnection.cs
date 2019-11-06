// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using Uno.SourceGeneration.Host.Helpers;
using Uno.SourceGeneration.Host.Messages;
using Uno.SourceGeneratorTasks.Helpers;
using static Uno.SourceGeneration.Host.Helpers.NativeMethods;

namespace Uno.SourceGeneration.Host.GenerationClient
{

	internal sealed class GenerationServerConnection
    {
		readonly static ILogger _log = typeof(GenerationServerConnection).Log();

        internal const string ServerNameDesktop = "Uno.SourceGeneration.Host.exe";
        internal const string ServerNameCoreClr = "Uno.SourceGeneration.Host.dll";

        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        internal const int TimeOutMsExistingProcess = 1000;

        // Spend up to 20s connecting to a new process, to allow time for it to start.
        internal const int TimeOutMsNewProcess = 20000;

        /// <summary>
        /// Determines if the compiler server is supported in this environment.
        /// </summary>
        internal static bool IsCompilerServerSupported(string tempPath)
        {
            var pipeName = GetPipeNameForPathOpt("");
            return pipeName != null && !IsPipePathTooLong(pipeName, tempPath);
        }

        public static Task<GenerationResponse> RunServerGeneration(
            string sharedCompilationId,
            List<string> arguments,
            GenerationsPathsInfo buildPaths,
            string keepAlive,
            CancellationToken cancellationToken)
        {
            var pipeNameOpt = sharedCompilationId ?? GetPipeNameForPathOpt(buildPaths.ClientDirectory);

            return RunServerGenerationCore(
                arguments,
                buildPaths,
                pipeNameOpt,
                keepAlive,
                timeoutOverride: null,
                tryCreateServerFunc: TryCreateServerCore,
                cancellationToken: cancellationToken);
        }

		internal bool TryShutdownGenerationServer(string pipeName, GenerationsPathsInfo buildPaths)
		{
			return TryCreateServerCore(buildPaths.ClientDirectory, pipeName, isShutdown: true);
		}

        internal static async Task<GenerationResponse> RunServerGenerationCore(
            List<string> arguments,
            GenerationsPathsInfo buildPaths,
            string pipeName,
            string keepAlive,
            int? timeoutOverride,
            Func<string, string, bool, bool> tryCreateServerFunc,
            CancellationToken cancellationToken)
        {
            if (pipeName == null)
            {
                return new RejectedGenerationResponse();
            }

            if (buildPaths.TempDirectory == null)
            {
                return new RejectedGenerationResponse();
            }

            // early check for the build hash. If we can't find it something is wrong; no point even trying to go to the server
            if (string.IsNullOrWhiteSpace(GenerationProtocolConstants.GetCommitHash()))
            {
                return new IncorrectHashGenerationResponse();
            }

            var clientDir = buildPaths.ClientDirectory;
            var timeoutNewProcess = timeoutOverride ?? TimeOutMsNewProcess;
            var timeoutExistingProcess = timeoutOverride ?? TimeOutMsExistingProcess;
            Task<NamedPipeClientStream> pipeTask = null;
            Mutex clientMutex = null;
            var holdsMutex = false;
            try
            {
                try
                {
                    var clientMutexName = GetClientMutexName(pipeName);
                    clientMutex = new Mutex(initiallyOwned: true, name: clientMutexName, out holdsMutex);
                }
                catch
                {
                    // The Mutex constructor can throw in certain cases. One specific example is docker containers
                    // where the /tmp directory is restricted. In those cases there is no reliable way to execute
                    // the server and we need to fall back to the command line.
                    //
                    // Example: https://github.com/dotnet/roslyn/issues/24124
                    return new RejectedGenerationResponse();
                }

                if (!holdsMutex)
                {
                    try
                    {
                        holdsMutex = clientMutex.WaitOne(timeoutNewProcess);

                        if (!holdsMutex)
                        {
                            return new RejectedGenerationResponse();
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        holdsMutex = true;
                    }
                }

                // Check for an already running server
                var serverMutexName = GetServerMutexName(pipeName);
                bool wasServerRunning = WasServerMutexOpen(serverMutexName);
                var timeout = wasServerRunning ? timeoutExistingProcess : timeoutNewProcess;

                if (wasServerRunning || tryCreateServerFunc(clientDir, pipeName, false))
                {
                    pipeTask = TryConnectToServerAsync(pipeName, timeout, cancellationToken);
                }
            }
            finally
            {
                if (clientMutex != null)
                {
                    if (holdsMutex)
                    {
                        clientMutex.ReleaseMutex();
                    }
                    clientMutex.Dispose();
                }
            }

            if (pipeTask != null)
            {
                var pipe = await pipeTask.ConfigureAwait(false);
                if (pipe != null)
                {
                    var request = GenerationRequest.Create(buildPaths.WorkingDirectory,
                                                      buildPaths.TempDirectory,
                                                      GenerationProtocolConstants.GetCommitHash(),
                                                      arguments,
                                                      keepAlive);

                    return await TryGeneration(pipe, request, cancellationToken).ConfigureAwait(false);
                }
            }

            return new RejectedGenerationResponse();
        }

        /// <summary>
        /// Try to compile using the server. Returns a null-containing Task if a response
        /// from the server cannot be retrieved.
        /// </summary>
        private static async Task<GenerationResponse> TryGeneration(NamedPipeClientStream pipeStream,
                                                            GenerationRequest request,
                                                            CancellationToken cancellationToken)
        {
			GenerationResponse response;
            using (pipeStream)
            {
                // Write the request
                try
                {
                    _log.Debug("Begin writing request");
                    await request.WriteAsync(pipeStream, cancellationToken).ConfigureAwait(false);
                    _log.Debug("End writing request");
                }
                catch (Exception e)
                {
					_log.Error($"Error writing build request. {e.Message}", e);
                    return new RejectedGenerationResponse();
                }

                // Wait for the compilation and a monitor to detect if the server disconnects
                var serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _log.Debug("Begin reading response");

                var responseTask = GenerationResponse.ReadAsync(pipeStream, serverCts.Token);
                var monitorTask = CreateMonitorDisconnectTask(pipeStream, "client", serverCts.Token);
                await Task.WhenAny(responseTask, monitorTask).ConfigureAwait(false);

                _log.Debug("End reading response");

                if (responseTask.IsCompleted)
                {
                    // await the task to log any exceptions
                    try
                    {
                        response = await responseTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
						_log.Error("Error reading response", e);
                        response = new RejectedGenerationResponse();
                    }
                }
                else
                {
                    _log.Debug("Server disconnect");
                    response = new RejectedGenerationResponse();
                }

                // Cancel whatever task is still around
                serverCts.Cancel();
                Debug.Assert(response != null);
                return response;
            }
        }

        /// <summary>
        /// The IsConnected property on named pipes does not detect when the client has disconnected
        /// if we don't attempt any new I/O after the client disconnects. We start an async I/O here
        /// which serves to check the pipe for disconnection.
        /// </summary>
        internal static async Task CreateMonitorDisconnectTask(
            PipeStream pipeStream,
            string identifier = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var buffer = Array.Empty<byte>();

            while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
            {
                // Wait a tenth of a second before trying again
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                try
                {
                    _log.Debug($"Before poking pipe {identifier}.");
                    await pipeStream.ReadAsync(buffer, 0, 0, cancellationToken).ConfigureAwait(false);
                    _log.Debug($"After poking pipe {identifier}.");
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
					// It is okay for this call to fail.  Errors will be reflected in the
					// IsConnected property which will be read on the next iteration of the
					_log.Error($"Error poking pipe {identifier}.", e);
                }
            }
        }

        /// <summary>
        /// Connect to the pipe for a given directory and return it.
        /// Throws on cancellation.
        /// </summary>
        /// <param name="pipeName">Name of the named pipe to connect to.</param>
        /// <param name="timeoutMs">Timeout to allow in connecting to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection to server.</param>
        /// <returns>
        /// An open <see cref="NamedPipeClientStream"/> to the server process or null on failure.
        /// </returns>
        internal static async Task<NamedPipeClientStream> TryConnectToServerAsync(
            string pipeName,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            NamedPipeClientStream pipeStream;
            try
            {
                // If the pipe path would be too long, there cannot be a server at the other end.
                // We're not using a saved temp path here because pipes are created with
                // Path.GetTempPath() in corefx NamedPipeClientStream and we want to replicate that behavior.
                if (IsPipePathTooLong(pipeName, Path.GetTempPath()))
                {
                    return null;
                }

                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the SHA1 of the directory the compiler exes live in as the pipe name.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                _log.Debug($"Attempt to open named pipe '{pipeName}'");

                pipeStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                _log.Debug($"Attempt to connect named pipe '{pipeName}'");
                try
                {
                    await pipeStream.ConnectAsync(timeoutMs, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (e is IOException || e is TimeoutException)
                {
                    // Note: IOException can also indicate timeout. From docs:
                    // TimeoutException: Could not connect to the server within the
                    //                   specified timeout period.
                    // IOException: The server is connected to another client and the
                    //              time-out period has expired.

                    _log.Debug($"Connecting to server timed out after {timeoutMs} ms");
                    return null;
                }
                _log.Debug($"Named pipe '{pipeName}' connected");

                cancellationToken.ThrowIfCancellationRequested();

                // Verify that we own the pipe.
                if (!CheckPipeConnectionOwnership(pipeStream))
                {
                    _log.Debug("Owner of named pipe is incorrect");
                    return null;
                }

                return pipeStream;
            }
            catch (Exception e) when (!(e is TaskCanceledException || e is OperationCanceledException))
            {
				_log.Error("Exception while connecting to process", e);
                return null;
            }
        }

        internal static bool TryCreateServerCore(string clientDir, string pipeName, bool isShutdown = false)
        {
            bool isRunningOnCoreClr = CoreClrShim.IsRunningOnCoreClr;
            string expectedPath;
            string processArguments;
            if (isRunningOnCoreClr)
            {
                // The server should be in the same directory as the client
                var expectedCompilerPath = Path.Combine(clientDir, ServerNameCoreClr);
                expectedPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
                processArguments = $@"""{expectedCompilerPath}"" ""-pipename:{pipeName}""";

                if (!File.Exists(expectedCompilerPath))
                {
                    return false;
                }
            }
            else
            {
                // The server should be in the same directory as the client
                expectedPath = Path.Combine(clientDir, ServerNameDesktop);
                processArguments = $@"""-pipename:{pipeName}""";

                if (!File.Exists(expectedPath))
                {
                    return false;
                }
            }

			if (isShutdown)
			{
				processArguments += "-shutdown";
			}

            if (PlatformInformation.IsWindows)
            {
                // As far as I can tell, there isn't a way to use the Process class to
                // create a process with no stdin/stdout/stderr, so we use P/Invoke.
                // This code was taken from MSBuild task starting code.

                STARTUPINFO startInfo = new STARTUPINFO();
                startInfo.cb = Marshal.SizeOf(startInfo);
                startInfo.hStdError = InvalidIntPtr;
                startInfo.hStdInput = InvalidIntPtr;
                startInfo.hStdOutput = InvalidIntPtr;
                startInfo.dwFlags = STARTF_USESTDHANDLES;
                uint dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW;

                PROCESS_INFORMATION processInfo;

                _log.Debug($"Attempting to create process '{expectedPath}'");

                var builder = new StringBuilder($@"""{expectedPath}"" {processArguments}");

                bool success = CreateProcess(
                    lpApplicationName: null,
                    lpCommandLine: builder,
                    lpProcessAttributes: NullPtr,
                    lpThreadAttributes: NullPtr,
                    bInheritHandles: false,
                    dwCreationFlags: dwCreationFlags,
                    lpEnvironment: NullPtr, // Inherit environment
                    lpCurrentDirectory: clientDir,
                    lpStartupInfo: ref startInfo,
                    lpProcessInformation: out processInfo);

                if (success)
                {
                    _log.Debug($"Successfully created process with process id {processInfo.dwProcessId}");
                    CloseHandle(processInfo.hProcess);
                    CloseHandle(processInfo.hThread);
                }
                else
                {
                    _log.Debug($"Failed to create process. GetLastError={Marshal.GetLastWin32Error()}");
                }
                return success;
            }
            else
            {
                try
				{
					_log.Debug($"Attempting to create process '{expectedPath}' '{processArguments}'");

					var startInfo = new ProcessStartInfo()
                    {
                        FileName = expectedPath,
                        Arguments = processArguments,
                        UseShellExecute = false,
                        WorkingDirectory = clientDir,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(startInfo);

					_log.Debug($"Successfully created process with process id {process.Id}");

					return true;
                }
                catch(Exception ex)
                {
					_log.Error($"Failed to create process.", ex);
					return false;
                }
            }
        }

        /// <summary>
        /// Check to ensure that the named pipe server we connected to is owned by the same
        /// user.
        /// </summary>
        /// <remarks>
        /// The type is embedded in assemblies that need to run cross platform.  While this particular
        /// code will never be hit when running on non-Windows platforms it does need to work when
        /// on Windows.  To facilitate that we use reflection to make the check here to enable it to
        /// compile into our cross plat assemblies.
        /// </remarks>
        private static bool CheckPipeConnectionOwnership(NamedPipeClientStream pipeStream)
        {
            try
            {
                if (PlatformInformation.IsWindows)
                {
                    var currentIdentity = WindowsIdentity.GetCurrent();
                    var currentOwner = currentIdentity.Owner;
                    var remotePipeSecurity = GetPipeSecurity(pipeStream);
                    var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));
                    return currentOwner.Equals(remoteOwner);
                }
                else
                {
                    return CheckIdentityUnix(pipeStream);
                }
            }
            catch (Exception ex)
            {
				_log.Error($"Error checking pipe connection {ex.Message}", ex);
                return false;
            }
        }

#if NET461 || NET472
        internal static bool CheckIdentityUnix(PipeStream stream)
        {
            // Identity verification is unavailable in the MSBuild task,
            // but verification is not needed client-side so that's okay.
            return true;
        }
#else
        [DllImport("System.Native", EntryPoint = "SystemNative_GetEUid")]
        private static extern uint GetEUid();

        [DllImport("System.Native", EntryPoint = "SystemNative_GetPeerID", SetLastError = true)]
        private static extern int GetPeerID(SafeHandle socket, out uint euid);

        internal static bool CheckIdentityUnix(PipeStream stream)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var handle = (SafePipeHandle)typeof(PipeStream).GetField("_handle", flags).GetValue(stream);
            var handle2 = (SafeHandle)typeof(SafePipeHandle).GetField("_namedPipeSocketHandle", flags).GetValue(handle);

            uint myID = GetEUid();

            if (GetPeerID(handle, out uint peerID) == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

			_log.Debug($"Checking pipe id (mine: {myID} peer:{peerID}");

			return myID == peerID;
        }
#endif

        private static ObjectSecurity GetPipeSecurity(PipeStream pipeStream)
        {
            return pipeStream.GetAccessControl();
        }

        /// <returns>
        /// Null if not enough information was found to create a valid pipe name.
        /// </returns>
        internal static string GetPipeNameForPathOpt(string serverExeDirectory)
        {
            var basePipeName = GetBasePipeName(serverExeDirectory);

            // Prefix with username and elevation
            bool isAdmin = false;
            if (PlatformInformation.IsWindows)
            {
                var currentIdentity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentIdentity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            var userName = Environment.UserName;
            if (userName == null)
            {
                return null;
            }

            return $"{userName}.{(isAdmin ? 'T' : 'F')}.{basePipeName}";
        }

        /// <summary>
        /// Check if our constructed path is too long. On some Unix machines the pipe is a
        /// real file in the temp directory, and there is a limit on how long the path can
        /// be. This will never be true on Windows.
        /// </summary>
        internal static bool IsPipePathTooLong(string pipeName, string tempPath)
        {
            if (PlatformInformation.IsUnix)
            {
                // This is the maximum path length of Unix Domain Sockets on a number of systems.
                // Since CoreFX implements named pipes using Unix Domain Sockets, if we exceed this
                // length than the pipe will fail.
                // This number is considered the smallest known max length according to
                // http://man7.org/linux/man-pages/man7/unix.7.html
                const int MaxPipePathLength = 92;
                const int PrefixLength = 11; // "CoreFxPipe_".Length
                return (tempPath.Length + PrefixLength + pipeName.Length) > MaxPipePathLength;
            }
            return false;
        }

        internal static string GetBasePipeName(string compilerExeDirectory)
        {
            // Normalize away trailing slashes.  File APIs include / exclude this with no 
            // discernable pattern.  Easiest to normalize it here vs. auditing every caller
            // of this method.
            compilerExeDirectory = compilerExeDirectory.TrimEnd(Path.DirectorySeparatorChar);

            string basePipeName;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(compilerExeDirectory));
                basePipeName = Convert.ToBase64String(bytes)
                    .Substring(0, 10) // We only have ~50 total characters on Mac, so strip this down
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }

            return basePipeName;
        }

        internal static bool WasServerMutexOpen(string mutexName)
        {
            try
            {
                Mutex mutex;
                var open = Mutex.TryOpenExisting(mutexName, out mutex);
                if (open)
                {
                    mutex.Dispose();
                    return true;
                }
            }
            catch
            {
                // In the case an exception occured trying to open the Mutex then 
                // the assumption is that it's not open. 
                return false;
            }

            return false;
        }

        internal static string GetServerMutexName(string pipeName)
        {
            return $"{pipeName}.server";
        }

        internal static string GetClientMutexName(string pipeName)
        {
            return $"{pipeName}.client";
        }

        /// <summary>
        /// Gets the value of the temporary path for the current environment assuming the working directory
        /// is <paramref name="workingDir"/>.  This function must emulate <see cref="Path.GetTempPath"/> as 
        /// closely as possible.
        /// </summary>
        public static string GetTempPath(string workingDir)
        {
            if (PlatformInformation.IsUnix)
            {
                // Unix temp path is fine: it does not use the working directory
                // (it uses ${TMPDIR} if set, otherwise, it returns /tmp)
                return Path.GetTempPath();
            }

            var tmp = Environment.GetEnvironmentVariable("TMP");
            if (Path.IsPathRooted(tmp))
            {
                return tmp;
            }

            var temp = Environment.GetEnvironmentVariable("TEMP");
            if (Path.IsPathRooted(temp))
            {
                return temp;
            }

            if (!string.IsNullOrEmpty(workingDir))
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    return Path.Combine(workingDir, tmp);
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    return Path.Combine(workingDir, temp);
                }
            }

            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (Path.IsPathRooted(userProfile))
            {
                return userProfile;
            }

            return Environment.GetEnvironmentVariable("SYSTEMROOT");
        }
    }
}
