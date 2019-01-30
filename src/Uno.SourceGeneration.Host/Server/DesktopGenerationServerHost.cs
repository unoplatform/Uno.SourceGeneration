// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Uno.SourceGeneration.Host.Server
{
    internal sealed class DesktopGenerationServerHost : GenerationServerHost
    {
        // Caches are used by C# and VB compilers, and shared here.
        public static readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> SharedAssemblyReferenceProvider =
			(path, properties) => new CachingMetadataReference(path, properties);

        public override Func<string, MetadataReferenceProperties, PortableExecutableReference> AssemblyReferenceProvider => SharedAssemblyReferenceProvider;

        internal DesktopGenerationServerHost(string clientDirectory)
            : base(clientDirectory)
        {
        }
    }
}
