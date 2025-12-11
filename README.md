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
</PropertyGroup>
```

The task runs automatically after compilation and generates a single file containing all your C# source files with namespaces removed and using statements consolidated.

By default, the output file is generated at `bin\SingleFile\{ProjectName}.cs`. For example, project `Kros.App` will produce `bin\SingleFile\Kros.App.cs`.

### Custom Output Location

You can customize the output folder and/or filename:

```xml
<PropertyGroup>
  <ProduceSingleCSharpFile>true</ProduceSingleCSharpFile>
  <SingleCSharpOutputFolder>custom\path</SingleCSharpOutputFolder>
  <SingleCSharpOutputFileName>MyScript.cs</SingleCSharpOutputFileName>
</PropertyGroup>
```

| Property | Default Value | Description |
|----------|---------------|-------------|
| `SingleCSharpOutputFolder` | `bin\SingleFile` | Output directory for the generated file |
| `SingleCSharpOutputFileName` | `{ProjectName}.cs` | Name of the generated file |

## Development

To test local changes, run the build script:

```bash
dotnet run build.cs
```

This script rebuilds the NuGet package, clears the cache, and builds the Demo project. The package is automatically built to `./nupkg` folder (configured in `nuget.config` as local feed).

---

## ⚠️ Disclaimer

This project is a **proof of concept**. The entire codebase was generated using AI. Use at your own risk. 🙂
