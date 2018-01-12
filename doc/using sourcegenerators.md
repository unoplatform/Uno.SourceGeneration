# Uno Source Generation


## msbuild properties

### SourceGeneratorInput
This property allows for a registered source generator (through `@SourceGenerator`) to specify non-`.cs` files to be considered
during incremental builds. If a generator uses `xml` files to generate content, use the following to ensure the generators will 
executed :

```xml
    <ItemGroup>
        <SourceGeneratorInput Include="$(MSBuildProjectDirectory)/**/*.xml" Exclude="bin/**/*.xml;obj/**/*.xml" />
    </ItemGroup>

