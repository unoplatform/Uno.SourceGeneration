// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// https://github.com/dotnet/roslyn/blob/3e39dd3535962bf9e30bd650e4ff34b610b8349a/src/Workspaces/Core/MSBuild/MSBuild/Constants/ItemNames.cs

namespace Uno.SourceGeneration.Engine.Workspace.Constants
{
    internal static class ItemNames
    {
        public const string AdditionalFiles = nameof(AdditionalFiles);
        public const string Analyzer = nameof(Analyzer);
        public const string Compile = nameof(Compile);
        public const string CscCommandLineArgs = nameof(CscCommandLineArgs);
        public const string DocFileItem = nameof(DocFileItem);
        public const string EditorConfigFiles = nameof(EditorConfigFiles);
        public const string Import = nameof(Import);
        public const string ProjectReference = nameof(ProjectReference);
        public const string Reference = nameof(Reference);
        public const string ReferencePath = nameof(ReferencePath);
        public const string VbcCommandLineArgs = nameof(VbcCommandLineArgs);
    }
}
