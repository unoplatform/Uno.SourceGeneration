// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// This file is inspired by the work the Roslyn compiler, adapter for source generation.
// Original source: https://github.com/dotnet/roslyn/commit/f15d8f701eee5a783b11e73d64b2e04f20ab64a7

using Roslyn.Utilities;
using System;
using System.Reflection;

namespace Uno.SourceGeneration.Host.Helpers
{
    /// <summary>
    /// Shim for APIs available only on CoreCLR.
    /// </summary>
    internal static class CoreClrShim
    {
        internal static bool IsRunningOnCoreClr => AssemblyLoadContext.Type != null;

        internal static class AssemblyLoadContext
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
               "System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        }

        internal static class AppContext
        {
            internal static readonly Type Type = ReflectionUtilities.TryGetType(
                "System.AppContext, System.AppContext, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            // only available in netstandard 1.6+
            internal static readonly Func<string, object> GetData =
                Type.GetTypeInfo().GetDeclaredMethod("GetData")?.CreateDelegate<Func<string, object>>();
        }
    }
}
