# Uno SourceGenerator
The Uno source generator is a API compatible source generator inspired 
by [Roslyn v2.0 source generation feature](https://github.com/dotnet/roslyn/blob/12bd769ebcd3121b88f535e8559f5a42d9c0e873/docs/features/generators.md), and an
msbuild task which executes the SourceGenerators.

It provides a way to generate C# source code based on a project being built, using all of its syntactic and semantic model information.

Using this generator allows for a set of generators to share the same Roslyn compilation context, which is particularly expensive to create, and run all the generators in parallel.

The `Uno.SourceGeneratorTasks` support updating generators on the fly, making iterative development easier as visual studio or MSBuild will not lock the generator's assemblies.

The `Uno.SourceGeneratorTasks` support any target framework for code generation, though there are limitations when [using a mixed targetframeworks graph](https://github.com/dotnet/roslyn/issues/23114), such as generating code in a `net47` project that references a `netstandard2.0` project. In such cases, prefer adding a `net47` target instead of targeting `netstandard2.0`.

## Creating a Source Generator

1. In Visual Studio 2017, create a **.NET Standard Class Library** project named `MyGenerator`
1. In the csproj file
	1. Change the TargetFramework to `net46`
	2. Add a package reference to `Uno.SourceGeneration` 
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

1. Create a file named `MyGenerator.props` in a folder named `build`
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

## Using the generator inside the same solution
1. In Visual Studio 2017, create a **.NET Standard Class Library** project named `MyLibrary`
1. In the csproj file
	1. Change the TargetFramework to `net46`
	1. Add a package reference to `Uno.SourceGenerationTasks` 
	```xml
	<ItemGroup>
		<PackageReference Include="Uno.SourceGenerationTasks" Version="1.5.0" />
	</ItemGroup>
	```
	1. Import the source generator by placing the following line at the end :
	```xml 
	<Import Project="..\MyGenerator\build\MyGenerator.props" />
	```
2. Add some C# code that uses the `MyGeneratedCode.TestGeneration` class that the generator creates.

## Packaging the source generator in NuGet
Packaging the generator in nuget requires to :

1. Add the `MyGenerator.props` file as build content :
	```xml
	<ItemGroup>
		<Content Include="build\MyGenerator.props">
			<Pack>true</Pack>
			<PackagePath>build</PackagePath>
		</Content>
	</ItemGroup>
	```
	Note that the name of this file must match the package name to be taken into account by nuget.

1. In the csproj
	1. Update the package references as follows
		```xml
		<ItemGroup>
			<PackageReference Include="Uno.SourceGeneration" Version="1.19.0-dev.316" PrivateAssets="All" />
			<PackageReference Include="Uno.SourceGenerationTasks" Version="1.19.0-dev.316" PrivateAssets="None" />
		</ItemGroup>
		```
		This ensure that the source generator tasks will be included in any project referencing our new generator, and that the source generation interfaces are not included.

	1. Add the following property
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
* Generators should have the least possible external dependencies. Generators are loaded in a separate `AppDomain` but multiple assemblies versions can be troublesome when loaded side by side.
* A generator currently cannot depend on another generator. When a project is loaded to be analyzed, all generated files are excluded from the roslyn `Compilation`, meaning that if two generators use the same conditions to generate the same code, there will be a compilation error in the resulting code.

## Troubleshooting
The source generator provides additional details when building, when running the `_UnoSourceGenerator` msbuild target. 

To view this information either place visual studio in `details` verbosity (**Options**, **Projects and Solutions**, **Build and Run** then **MSBuild project build output verbosity**) or by using the excellent [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/) from [Kirill Osenkov](https://twitter.com/KirillOsenkov).