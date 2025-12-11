#:sdk Microsoft.NET.Sdk
#:package CliWrap@3.8.2

using CliWrap;

Console.WriteLine("🔨 Building Kros.SingleCsFileGenerator...");
await Cli.Wrap("dotnet")
    .WithArguments("build Kros.SingleCsFileGenerator/Kros.SingleCsFileGenerator.csproj -c Release")
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

Console.WriteLine("🧹 Clearing NuGet cache...");
await Cli.Wrap("dotnet")
    .WithArguments("nuget locals all --clear")
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .ExecuteAsync();

Console.WriteLine("🔨 Building Demo project...");
await Cli.Wrap("dotnet")
    .WithArguments("build Kros.SingleCsFileGenerator.Demo/Kros.SingleCsFileGenerator.Demo.csproj")
    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()))
    .ExecuteAsync();

Console.WriteLine("✅ Done!");
