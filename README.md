# Kros.SingleCsFileGenerator

MSBuild task that merges standard C# project with multiple files into a single file compatible with .NET 10's
[file-based apps](https://devblogs.microsoft.com/dotnet/announcing-dotnet-run-app/) feature.

.NET 10 introduced the ability to run single C# files directly using `dotnet run app.cs`, but these single files cannot
reference other source files. This tool allows you to develop a more complex multi-file project, then generate a single
file from it. This can be used in DevOps pipelines, automation scripts, or anywhere you need a single-file C# script.

## Installation

Add the NuGet package to your project:

```bash
dotnet package add Kros.SingleCsFileGenerator
```

## Usage

Enable the task in your `.csproj` file:

```xml
<PropertyGroup>
  <GenerateCSharpSingleFile>true</GenerateCSharpSingleFile>
</PropertyGroup>
```

The task runs automatically after compilation and generates a single file containing all your C# source files with namespaces
removed and using statements consolidated.

By default, the output file is created at `bin/SingleFile/{ProjectName}.cs`.
For example, project `Kros.App` will create `bin/SingleFile/Kros.App.cs`.

### Custom output location

You can customize the output folder and/or filename:

```xml
<PropertyGroup>
  <GenerateCSharpSingleFile>true</GenerateCSharpSingleFile>
  <CSharpSingleFileOutputFolder>custom/path</CSharpSingleFileOutputFolder>
  <CSharpSingleFileOutputFileName>MyScript.cs</CSharpSingleFileOutputFileName>
</PropertyGroup>
```

| Property                         | Default Value      | Description                           |
|----------------------------------|--------------------|---------------------------------------|
| `CSharpSingleFileOutputFolder`   | `bin/SingleFile`   | Output folder for the generated file. |
| `CSharpSingleFileOutputFileName` | `MyScript.cs`      | Name of the generated file.           |

## Development

NuGet package of `Kros.SingleCsFileGenerator` is generated automatically during build in the `nupkg` thisfolder at the solution level.
This folder is also set as local NuGet source. Project `Kros.SingleCsFileGenerator.Demo` references `Kros.SingleCsFileGenerator`
NuGet and is configured to reference latest version including prereleases. If you (re)build solution, single files app
`Kros.SingleCsFileGenerator.Demo.cs` is generated in `Kros.SingleCsFileGenerator.Demo/bin/SingleFile` folder.

To make changes to `Kros.SingleCsFileGenerator` and test them in the demo project, you have always use new version of
the generator. The simplest way is temporarily change version in `Kros.SingleCsFileGenerator.csproj` file:

``` xml
<PropertyGroup>
	<VersionSuffix>$([System.DateTime]::UtcNow.ToString('yyyyMMdd.HHmmss'))</VersionSuffix>
</PropertyGroup>
```

This way, you will have new version of NuGet **for every build** and demo project will automatically restore and use it.

If you want to test just the generation of single file without creating new NuGet package, you can use
`Kros.SingleCsFileGenerator.Runner` – and you can run it as single file application. 😉
It has two mandatory arguments. First is path to project (`.csproj` file) to generate single file from and second is
output path for generated single file. You can use any project you want, not just demo project in this solution.

``` sh
dotnet run ./Kros.SingleCsFileGenerator.Runner.cs ../Kros.SingleCsFileGenerator.Demo/Kros.SingleCsFileGenerator.Demo.csproj ../test.cs
```
