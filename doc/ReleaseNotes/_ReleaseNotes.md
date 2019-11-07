# Release notes

## Next version

### Features
- Introduction of the `UnoSourceGeneratorAdditionalProperty` item group allows for the propagation of those properties to the generators. (#112)

### Breaking changes
* This update removes unused dependencies, to improve the nuget restore time :
    * Microsoft.Build.Engine
    * Microsoft.Build.Tasks.Core
    * Microsoft.CodeAnalysis
  You may have to add those dependencies manually if your code relied in those, or one of their dependencies.
- 

### Bug fixes
- 

## Release 1.31.0

No major changes

## Release 1.30.0

### Bug fixes

- Add more information when the Roslyn Compilation fails

- Fix UWP Compilation

- Fix roslyn MetadataReference performance

- Expose single-use host mode parameter

- Fix for Hosted mode not selected on .NET desktop CLI

## Release 1.29.0

### Features
- Improve error message when a cross targeted project is misconfigured (#25)
- Add Linux build support
- Nuget packages are now signed with Authenticode.
- Support for VS2019 16.0 Pre 1
- Pass-through support for F# and VB.NET projects

### Breaking changes
- `uap10.0` projects must be updated to use the `uap10.0.xxx` target framework format

### Bug fixes
- Added workaround for missing designer file error with Xamarin.Android projects
