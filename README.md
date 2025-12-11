# Kros.SingleCsFileGenerator

MSBuild task that merges multiple C# source files into a single amalgamated file compatible with .NET 10's [file-based apps](https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/) feature.

.NET 10 introduced the ability to run single C# files directly using `dotnet run app.cs`, but these single files cannot reference other source files. This tool allows you to develop a more complex multi-file project, then generate a single amalgamated file that can be used in DevOps pipelines, automation scripts, or anywhere you need a single-file C# script.

## Installation

Add the NuGet package to your project:

```bash
dotnet package add Kros.SingleCsFileGenerator
```

## Usage

Enable the task in your `.csproj` file:

```xml
<PropertyGroup>
  <ProduceSingleCSharpFile>true</ProduceSingleCSharpFile>
  <SingleCSharpOutputFile>$(MSBuildProjectDirectory)\app.gen.cs</SingleCSharpOutputFile>
</PropertyGroup>
```

The task runs automatically after compilation and generates a single file containing all your C# source files with namespaces removed and using statements consolidated.
