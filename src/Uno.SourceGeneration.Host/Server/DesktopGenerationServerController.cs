// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.SourceGeneration.Host.GenerationClient;
using Uno.SourceGeneratorTasks.Helpers;

namespace Uno.SourceGeneration.Host.Server
{
	internal class DesktopGenerationServerController : GenerationServerController
	{
		internal const string KeepAliveSettingName = "keepalive";

		private readonly NameValueCollection _appSettings;

		internal DesktopGenerationServerController(NameValueCollection appSettings)
		{
			_appSettings = appSettings;
		}

		protected override IClientConnectionHost CreateClientConnectionHost(string pipeName)
		{
			var compilerServerHost = CreateCompilerServerHost();
			return CreateClientConnectionHostForServerHost(compilerServerHost, pipeName);
		}

		internal static IGenerationServerHost CreateCompilerServerHost()
		{
			// VBCSCompiler is installed in the same directory as csc.exe and vbc.exe which is also the 
			// location of the response files.
			var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;

			return new DesktopGenerationServerHost(clientDirectory);
		}

		internal static IClientConnectionHost CreateClientConnectionHostForServerHost(
			IGenerationServerHost compilerServerHost,
			string pipeName)
		{
			return new NamedPipeClientConnectionHost(compilerServerHost, pipeName);
		}

		protected internal override TimeSpan? GetKeepAliveTimeout()
		{
			try
			{
				int keepAliveValue;
				string keepAliveStr = _appSettings[KeepAliveSettingName];
				if (int.TryParse(keepAliveStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out keepAliveValue) &&
					keepAliveValue >= 0)
				{
					if (keepAliveValue == 0)
					{
						// This is a one time server entry.
						return null;
					}
					else
					{
						return TimeSpan.FromSeconds(keepAliveValue);
					}
				}
				else
				{
					return ServerDispatcher.DefaultServerKeepAlive;
				}
			}
			catch (Exception e)
			{
				this.Log().Error("Could not read AppSettings", e);
				return ServerDispatcher.DefaultServerKeepAlive;
			}
		}

		protected override async Task<Stream> ConnectForShutdownAsync(string pipeName, int timeout)
		{
			return await GenerationServerConnection.TryConnectToServerAsync(pipeName, timeout, cancellationToken: default).ConfigureAwait(false);
		}

		protected override string GetDefaultPipeName()
		{
			var clientDirectory = AppDomain.CurrentDomain.BaseDirectory;
			return GenerationServerConnection.GetPipeNameForPathOpt(clientDirectory);
		}

		protected override bool? WasServerRunning(string pipeName)
		{
			string mutexName = GenerationServerConnection.GetServerMutexName(pipeName);
			return GenerationServerConnection.WasServerMutexOpen(mutexName);
		}

		protected override int RunServerCore(string pipeName, IClientConnectionHost connectionHost, IDiagnosticListener listener, TimeSpan? keepAlive, CancellationToken cancellationToken)
		{
			// Grab the server mutex to prevent multiple servers from starting with the same
			// pipename and consuming excess resources. If someone else holds the mutex
			// exit immediately with a non-zero exit code
			var mutexName = GenerationServerConnection.GetServerMutexName(pipeName);
			bool holdsMutex;
			using (var serverMutex = new Mutex(initiallyOwned: true,
												name: mutexName,
												createdNew: out holdsMutex))
			{
				if (!holdsMutex)
				{
					return CommonGenerator.Failed;
				}

				try
				{
					return base.RunServerCore(pipeName, connectionHost, listener, keepAlive, cancellationToken);
				}
				finally
				{
					serverMutex.ReleaseMutex();
				}
			}
		}

		internal static new int RunServer(
			string pipeName,
			string tempPath,
			IClientConnectionHost clientConnectionHost = null,
			IDiagnosticListener listener = null,
			TimeSpan? keepAlive = null,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			GenerationServerController controller = new DesktopGenerationServerController(new NameValueCollection());
			return controller.RunServer(pipeName, tempPath, clientConnectionHost, listener, keepAlive, cancellationToken);
		}
	}
}
