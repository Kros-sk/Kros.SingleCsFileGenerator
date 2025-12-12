using Kros.SingleCsFileGenerator;
using Microsoft.Build.Framework;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

args = NormalizeArguments(args);

Argument<FileInfo> projectArgument = new(name: "project")
{
    Arity = ArgumentArity.ExactlyOne,
    Description = "Path to C# project (.csproj) file."
};

Argument<FileInfo> outputArgument = new(name: "output")
{
    Arity = ArgumentArity.ExactlyOne,
    Description = "Path where single-file application will be generated."
};

Option<bool> enableTrimmingOption = new("--enableTrimming")
{
    Arity = ArgumentArity.ZeroOrOne,
    Description = "If set, trimming is enabled. Default is disabled."
};

RootCommand rootCommand = new("Generates single-file application from given C# .NET project.")
{
    projectArgument,
    outputArgument,
    enableTrimmingOption
};

rootCommand.Action = new ProjectCommandAction(projectArgument, outputArgument, enableTrimmingOption);

return rootCommand.Parse(args).Invoke();

// This is to support running compiled binary as well as using 'dotnet run' command.
// When using 'dotnet run', the first argument is the path to main .cs file, so I will remove it from the argument list.
// This way we can run this program both ways:
//   1. dotnet run Kros.SingleCsFileGenerator.Runner.cs path/to/project.csproj
//   2. Kros.SingleCsFileGenerator.Runner.exe path/to/project.csproj
static string[] NormalizeArguments(string[] args)
{
    if ((args.Length > 0) && args[0].EndsWith("Kros.SingleCsFileGenerator.Runner.cs", StringComparison.Ordinal))
    {
        return args[1..];
    }
    return args;
}

static class ExitCodes
{
    public const int Success = 0;
    public const int InvalidFileExtension = 1;
    public const int FileNotFound = 2;
    public const int InvalidFileFormat = 3;
    public const int GenerationError = 4;
}

sealed class ProjectCommandAction(
    Argument<FileInfo> projectArgument,
    Argument<FileInfo> outputArgument,
    Option<bool> enableTrimmingOption)
    : SynchronousCommandLineAction
{
    private sealed class TaskItem(string itemSpec, string version) : ITaskItem
    {
        private readonly string _version = version ?? string.Empty;

        public string ItemSpec { get; set; } = itemSpec;

        public string GetMetadata(string metadataName)
            => metadataName switch
            {
                "FullPath" => ItemSpec,
                "Version" => _version,
                _ => string.Empty
            };

        public ICollection MetadataNames { get; } = Array.Empty<object>();
        public int MetadataCount { get; } = 0;
        public IDictionary CloneCustomMetadata() => throw new NotImplementedException();
        public void CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException();
        public void RemoveMetadata(string metadataName) => throw new NotImplementedException();
        public void SetMetadata(string metadataName, string metadataValue) => throw new NotImplementedException();
    }

    private sealed class DummyBuildEngine : IBuildEngine
    {
        public void LogMessageEvent(BuildMessageEventArgs e) => Console.WriteLine($"INFO: {e.Message}");
        public void LogWarningEvent(BuildWarningEventArgs e) => Console.WriteLine($"WARN: {e.Message}");
        public void LogErrorEvent(BuildErrorEventArgs e) => Console.WriteLine($"ERR: {e.Message}");
        public void LogCustomEvent(CustomBuildEventArgs e) => Console.WriteLine($"CUST: {e.Message}");

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            IDictionary globalProperties,
            IDictionary targetOutputs)
            => throw new NotImplementedException();
        public bool ContinueOnError => throw new NotImplementedException();
        public int LineNumberOfTaskNode => throw new NotImplementedException();
        public int ColumnNumberOfTaskNode => throw new NotImplementedException();
        public string ProjectFileOfTaskNode => throw new NotImplementedException();
    }

    public override int Invoke(ParseResult parseResult)
    {
        FileInfo projectFile = parseResult.GetValue(projectArgument)!;
        FileInfo outputFile = parseResult.GetValue(outputArgument)!;
        if (!ProcessProject(projectFile, out XElement? root, out int exitCode))
        {
            return exitCode;
        }
        (string sdk, string rootNamespace, ITaskItem[] packageReferences, ITaskItem[] sourceFiles)
            = LoadProjectData(projectFile, root);

        bool enableTrimming = parseResult.GetValue(enableTrimmingOption);
        GenerateCSharpSingleFileTask generator = new()
        {
            ProjectName = Path.GetFileNameWithoutExtension(projectFile.Name),
            ProjectSdk = sdk,
            RootNamespace = rootNamespace,
            PackageReferences = packageReferences,
            SourceFiles = sourceFiles,
            OutputFile = outputFile.FullName,
            EnableTrimming = enableTrimming,
            BuildEngine = new DummyBuildEngine()
        };

        try
        {
            generator.Execute();
            exitCode = ExitCodes.Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error when generating single-file app: {ex.Message}");
            exitCode = ExitCodes.GenerationError;
        }

        return exitCode;
    }

    private static bool ProcessProject(FileInfo projectFile, [NotNullWhen(true)] out XElement? root, out int exitCode)
    {
        root = null;
        exitCode = 0;
        if (projectFile.Extension != ".csproj")
        {
            Console.Error.WriteLine($"Error: File must be a .csproj file: {projectFile.FullName}");
            exitCode = ExitCodes.InvalidFileExtension;
        }
        else if (!projectFile.Exists)
        {
            Console.Error.WriteLine($"Error: Project file not found: {projectFile.FullName}");
            exitCode = ExitCodes.FileNotFound;
        }
        else
        {
            XDocument doc = XDocument.Load(projectFile.FullName);
            root = doc.Root;
            if (root is null)
            {
                Console.Error.WriteLine("Error: Invalid project file format.");
                exitCode = ExitCodes.InvalidFileFormat;
            }
        }
        return root is not null;
    }

    private static (string sdk, string rootNamespace, ITaskItem[] packageReferences, ITaskItem[] sourceFiles) LoadProjectData(
        FileInfo projectFile,
        XElement root)
    {
        string sdk = root.Attribute("Sdk")?.Value ?? string.Empty;
        string rootNamespace = root.Descendants("RootNamespace").FirstOrDefault()?.Value ?? string.Empty;
        ITaskItem[] packageReferences = [.. root.Descendants("PackageReference")
            .Select(packageRef => new TaskItem(
                packageRef.Attribute("Include")?.Value ?? string.Empty,
                packageRef.Attribute("Version")?.Value ?? string.Empty))];

        string binFolder = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        string objFolder = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        ITaskItem[] sourceFiles = [.. Directory.GetFiles(projectFile.DirectoryName!, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(binFolder) && !file.Contains(objFolder))
            .OrderBy(file => file)
            .Select(file => new TaskItem(file, string.Empty))];

        return (sdk, rootNamespace, packageReferences, sourceFiles);
    }
}
