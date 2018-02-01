# Uno SourceGenerator
The Uno source generator is an API compatible source generator inspired 
by [Roslyn v2.0 source generation feature](https://github.com/dotnet/roslyn/blob/12bd769ebcd3121b88f535e8559f5a42d9c0e873/docs/features/generators.md), and an
msbuild task which executes the SourceGenerators.

It provides a way to generate C# source code based on a project being built, using all of its syntactic and semantic model information.

Using this generator allows for a set of generators to share the same Roslyn compilation context, which is particularly expensive to create, and run all the generators in parallel.

The `Uno.SourceGeneratorTasks` support updating generators on the fly, making iterative development easier as visual studio or MSBuild will not lock the generator's assemblies.

The `Uno.SourceGeneratorTasks` support any target framework for code generation, though there are limitations when [using a mixed targetframeworks graph](https://github.com/dotnet/roslyn/issues/23114), such as generating code 
in a `net47` project that references a `netstandard2.0` project. In such cases, prefer adding a `net47` target instead of targeting `netstandard2.0`.

Visual Studio 2017 15.3+ for Windows and macOS are supported.

## Build status

| Target | Branch | Status | Recommended Nuget packages version |
| ------ | ------ | ------ | ------ |
| development | master |[![Build status](https://ci.appveyor.com/api/projects/status/0jsq4wg0ce7a5rqu/branch/master?svg=true)](https://ci.appveyor.com/project/nventivedevops/uno-sourcegeneration/branch/master) | [![NuGet](https://img.shields.io/nuget/v/Uno.SourceGenerationTasks.svg)](https://www.nuget.org/packages/Uno.SourceGenerationTasks/) [![NuGet](https://img.shields.io/nuget/v/Uno.SourceGeneration.svg)](https://www.nuget.org/packages/Uno.SourceGeneration/) |

## Creating a Source Generator

1. In Visual Studio 2017, create a **.NET Standard Class Library** project named `MyGenerator`
1. In the csproj file
	1. Change the TargetFramework to `net46`
	2. Add a package reference to `Uno.SourceGeneration` (take the latest version)
	```xml
	<ItemGroup>
		<PackageReference Include="Uno.SourceGeneration" Version="1.5.0" />
	</ItemGroup>
	```
1. Add a new source file containing this code :
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

1. Create a file named `MyGenerator.props` (should be the name of your project + `.props`) in a folder named
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
* A generator currently cannot depend on another generator. When a project is loaded to be
  analyzed, all generated files are excluded from the roslyn `Compilation`, meaning that if
  two generators use the same conditions to generate the same code, there will be a compilation
  error in the resulting code.
* If you need a generator to use the result of another one for its own compilation, you can use
  the `[SourceGeneratorDependency]` attribute. You simply need to specify the FullName
  (namespace + type name) of another generator.  If this generator is found, it will ensure it
  is executed before and the result is added to the compilation before calling yours.
* Sometimes you may need to kill all instances of MsBuild. On Windows, the fatest way to to that
  is to open a shell in admin mode and type this line:
  ```
  taskkill /fi "imagename eq msbuild.exe" /f /t
  ```


## Troubleshooting
The source generator provides additional details when building, when running the `_UnoSourceGenerator` msbuild target. 

To view this information either place visual studio in `details` verbosity (**Options**, **Projects and Solutions**, **Build and Run** then **MSBuild project build output verbosity**) or by using the excellent [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) from [Kirill Osenkov](https://twitter.com/KirillOsenkov).