# Release notes

## Next version

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
