// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Uno.SourceGeneration.Host.GenerationClient
{
	internal struct GenerationsPathsInfo
    {
        /// <summary>
        /// The path which contains the compiler binaries and response files.
        /// </summary>
        internal string ClientDirectory { get; }

        /// <summary>
        /// The path in which the compilation takes place.
        /// </summary>
        internal string WorkingDirectory { get; }

        /// <summary>
        /// The temporary directory a compilation should use instead of <see cref="Path.GetTempPath"/>.  The latter
        /// relies on global state individual compilations should ignore.
        /// </summary>
        internal string TempDirectory { get; }

        internal GenerationsPathsInfo(string clientDir, string workingDir, string tempDir)
        {
            ClientDirectory = clientDir;
            WorkingDirectory = workingDir;
            TempDirectory = tempDir;
        }
    }
}
