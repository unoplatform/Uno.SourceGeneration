# Uno SourceGenerator

The Uno source generator is an API compatible source generator inspired
by [Roslyn v2.0 source generation feature](https://github.com/dotnet/roslyn/blob/12bd769ebcd3121b88f535e8559f5a42d9c0e873/docs/features/generators.md), and an
msbuild task which executes the SourceGenerators.

It provides a way to generate C# source code based on a project being built, using all of its syntactic and semantic model information.

Using this generator allows for a set of generators to share the same Roslyn compilation context, which is particularly expensive to create, and run all the generators in parallel.

The `Uno.SourceGeneratorTasks` support updating generators on the fly, making iterative development easier as visual studio or MSBuild will not lock the generator's assemblies.

The `Uno.SourceGeneratorTasks` support any target framework for code generation, though there are limitations when [using a mixed targetframeworks graph](https://github.com/dotnet/roslyn/issues/23114), such as generating code
in a `net47` project that references a `netstandard2.0` project. In such cases, prefer adding a `net47` target instead of targeting `netstandard2.0`.

Visual Studio 2017 15.3+ for Windows, macOS and Linux builds are supported. Building for .NET Core requires .NET 3.0.100 or later.

## Build status

| Target | Branch | Status |
| ------ | ------ | ------ |
| development | master |[![Build Status](https://dev.azure.com/uno-platform/Uno%20Platform/_apis/build/status/Uno%20Platform/Uno.SourceGeneration%20CI?branchName=master)](https://dev.azure.com/uno-platform/Uno%20Platform/_build/latest?definitionId=32&branchName=master)

## Nuget Packages

| Package | nuget.org | usage |
| -- | -- | -- |
| `Uno.SourceGeneration` | [![NuGet](https://img.shields.io/nuget/v/Uno.SourceGeneration.svg)](https://www.nuget.org/packages/Uno.SourceGeneration/) | Use this package to create a generator |
| `Uno.SourceGenerationTasks` | [![NuGet](https://img.shields.io/nuget/v/Uno.SourceGenerationTasks.svg)](https://www.nuget.org/packages/Uno.SourceGenerationTasks/) | Use this package to use a generator |

Experimental packages are available through this NuGet feed: https://pkgs.dev.azure.com/uno-platform/Uno%20Platform/_packaging/unoplatformdev/nuget/v3/index.json

## Creating a Source Generator

1. In Visual Studio 2017, create a **.NET Standard Class Library** project named `MyGenerator`
2. In the csproj file
    1. Change the TargetFramework to `net46`
    2. Add a package reference to `Uno.SourceGeneration` (take the latest version)
    ```xml
    <ItemGroup>
        <PackageReference Include="Uno.SourceGeneration" Version="1.5.0" />
    </ItemGroup>
    ```
3. Add a new source file containing this code :
    ```csharp
    using System;
    using Uno.SourceGeneration;

    namespace MyGenerator
    {
        public class MyCustomSourceGenerator : SourceGenerator
        {
            public override void Execute(SourceGeneratorContext context)
            {
                var project = context.GetProjectInstance();

                context.AddCompilationUnit("Test", "namespace MyGeneratedCode { class TestGeneration { } }");
            }
        }
    }
    ```
    Note that the GetProjectInstance is a helper method that provides access to the msbuild project currently being built. It provides access to the msbuild properties and item groups of the project, allowing for fine configuration of the source generator.

4. Create a file named `MyGenerator.props` (should be the name of your project + `.props`) in a folder named
   `build` and set its _Build Action_ to `Content`. Put the following content:
    ```xml
    <Project>
        <ItemGroup>
            <SourceGenerator Include="$(MSBuildThisFileDirectory)..\bin\$(Configuration)\net46\MyGenerator.dll"
                    Condition="Exists('$(MSBuildThisFileDirectory)..\bin')" />
            <SourceGenerator Include="$(MSBuildThisFileDirectory)..\tools\MyGenerator.dll"
                    Condition="Exists('$(MSBuildThisFileDirectory)..\tools')" />
        </ItemGroup>
    </Project>
    ```

## Using the generator inside the same solution (another project)

1. In Visual Studio 2017, create a **.NET Standard Class Library** project named `MyLibrary`.
   This is the project where your generator will do its generation.
1. In the `.csproj` file:
    1. Change the TargetFramework to `net46` (.Net Framework v4.6)
    1. Add a package reference to `Uno.SourceGenerationTasks`
    ```xml
    <ItemGroup>
        <PackageReference Include="Uno.SourceGenerationTasks" Version="1.5.0" />
    </ItemGroup>
    ```
    > *You can also use the Nuget Package Manager to add this package reference.
    > **The version can differ, please use the same than the generator project**.
    1. Import the source generator by placing the following line at the end :
    ```xml
    <Import Project="..\MyGenerator\build\MyGenerator.props" />
    ```
1. Add some C# code that uses the `MyGeneratedCode.TestGeneration` class that the generator creates.
1. Compile... it should works.
1. You can sneak at the generated code by clicking the _Show All Files_ button in the _Solution Explorer_.
   The code will be in the folder `obj\<config>\<platform>\g\<generator name>\`.

## Packaging the source generator in NuGet

Packaging the generator in nuget requires to :

1. In the `csproj` file containing your generator:
   1. Add this group to your csproj:
    ```xml
    <ItemGroup>
        <Content Include="build/**/*.*">
        <Pack>true</Pack>
        <PackagePath>build</PackagePath>
        </Content>
    </ItemGroup>

    ```

    Note that the name of this file must match the package name to be taken into account by nuget.

    1. Update the package references as follows
        ```xml
        <ItemGroup>
            <PackageReference Include="Uno.SourceGeneration" Version="1.19.0-dev.316" PrivateAssets="All" />
            <PackageReference Include="Uno.SourceGenerationTasks" Version="1.19.0-dev.316" PrivateAssets="None" />
        </ItemGroup>
        ```
        This ensure that the source generator tasks will be included in any project referencing your
        new generator, and that the source generation interfaces are not included.

        > *You can also use the Nuget Package Manager to add this package reference.
        > **The version can differ, please take the latest stable one**.

    1. Add the following property:
        ```xml
        <PropertyGroup>
            <IsTool>true</IsTool>
        </PropertyGroup>
        ```
        This will allow for the generator package to be installed on any target framework.

## Debugging a generator

In your generator, add the following in the `SourceGenerator.Execute` override :

```csharp
Debugger.Launch();
```

This will open another visual studio instance, and allow for stepping through the generator's code.

## General guidelines for creating generators

* Generators should have the least possible external dependencies.
  Generators are loaded in a separate `AppDomain` but multiple assemblies versions can be
  troublesome when loaded side by side.

* You can add a dependency on your generator by adding the `Uno.SourceGeneration.SourceGeneratorDependency`
  attribute on your class:

  ```csharp
  [GenerateAfter("Uno.ImmutableGenerator")] // Generate ImmutableGenerator before EqualityGenerator
  public class EqualityGenerator : SourceGenerator
  ```

  For instance here, it will ensure that the `ImmutableGenerator` is executed before your `EqualityGenerator`.
  If you don't declare those dependencies, when a project is loaded to be analyzed, all generated files
  from a generator are excluded from the roslyn `Compilation` object of other generators, meaning that if
  two generators use the same conditions to generate the same code, there will be a compilation
  error in the resulting code.

* You can also define a generator which must be executed **after** yours. To do this, you need to declare a
  _dependent generator_:

  ```csharp
  [GenerateBefore("Uno.EqualityGenerator")] // Generate ImmutableGenerator before EqualityGenerator
  public class ImmutableGenerator : SourceGenerator
  ```

* Sometimes you may need to kill all instances of MsBuild. On Windows, the fatest way to to that
  is to open a shell in admin mode and type this line:

  ``` powershell
  taskkill /fi "imagename eq msbuild.exe" /f /t
  ```

## Logging to build output

You can write to build output using the following code:

``` csharp
    public override void Execute(SourceGeneratorContext context)
    {
        var logger = context.GetLogger(); // this is an extension method on SourceGeneratorContext

        // Log something to build output when the mode is "detailed".
        logger.Debug($"The current count is {count}");

        // Log something to build output when the mode is "normal".
        logger.Info($"A total of {filesCount} has been generated.");

        // Log something as warning in build output.
        logger.Warn($"No code generated because the mode is {currentMode}.");

        // Log something as error in build output.
        logger.Error($"Unable to open file {filename}. No code generated.");
    }
```

## Available Properties

The source generation task provides set of properties that can alter its behavior based on your project.

### UnoSourceGeneratorAdditionalProperty

The `UnoSourceGeneratorAdditionalProperty` item group provides the ability for a project to enable the
propagation of specific properties to the generators. This may be required if properties are 
[injected dynamically](https://github.com/Microsoft/VSProjectSystem/blob/master/doc/extensibility/IProjectGlobalPropertiesProvider.md#iprojectglobalpropertiesprovider),
or provided as global variables.

A good example of this is the `JavaSdkDirectory` that is generally injected as a global parameter through
the msbuild command line.

In such as case, add the following in your project file:

```xml
<ItemGroup>
    <UnoSourceGeneratorAdditionalProperty Include="JavaSdkDirectory" />
</ItemGroup>
```

In this case, the `JavaSdkDirectory` value will be captured in the original build environment, and provided
to the generators' build environment.

## Troubleshooting

### Error: `Failed to analyze project file ..., the targets ... failed to execute.`

This is issue is caused by a [open Roslyn issue](https://github.com/nventive/Uno.SourceGeneration/issues/2)
for which all projects of the solution must have all the possible "head" projects configuration.

For instance, if you are building a UWP application, all the projects in the solution must
have the `10.0.xxx` target framework defined, even if `netstandard2.0` would have been enough.

### Generation output

The source generator provides additional details when building, when running the `_UnoSourceGenerator` msbuild target.

To view this information either place visual studio in `details` verbosity (**Options**, **Projects and Solutions**,
**Build and Run** then **MSBuild project build output verbosity**) or by using the excellent
[MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) from [Kirill Osenkov](https://twitter.com/KirillOsenkov).

The source generation target can also optionally produces `binlog` file in the obj folder, used to troubleshoot issues with the msbuild instance
created inside the application domain used to generate the code. The path for those files can be altered using the
`UnoSourceGeneratorBinLogOutputPath` msbuild property. In the context of a VSTS build, setting it to `$(build.artifactstagingdirectory)`
allows for an improved diagnostics experience. Set the `UnoSourceGeneratorUnsecureBinLogEnabled` property to true to enabled binary logging.

> **Important**: The binary logger may leak secret environment variables, it is a best practice to never enable this feature as part of normal build.

### My build ends with error code 3

By default, in some cases, the source generation host will run into an internal error, and will exit without providing details about the generation error.

To enable the logging of these errors, add the following property to your project:
```xml
<UnoSourceGeneratorCaptureGenerationHostOutput>true</UnoSourceGeneratorCaptureGenerationHostOutput>
```

The errors will the be visible when the build logging output is set to detailed, or by [using the binary logger](https://github.com/Microsoft/msbuild/blob/master/documentation/wiki/Binary-Log.md).

# Have questions? Feature requests? Issues?

Make sure to visit our [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform), [create an issue](https://github.com/nventive/Uno.SourceGeneration/issues) or [visit our gitter](https://gitter.im/uno-platform/Lobby).


## Upgrade notes

### earlier versions to 1.29
A breaking change has been introduced to support proper UWP head projects, and when upgrading to Uno.SourceGenerationTasks `1.29` or later
you will have to search for projects that contain the `uap10.0` target framework and do the following:
- Update to the MSBuild.Sdk.Extras 1.6.65 or later
- Choose an UWP sdk version, and use the appropriate target framework (e.g. `uap10.0` to `uap10.0.16299`)
