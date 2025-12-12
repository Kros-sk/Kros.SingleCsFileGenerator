using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kros.SingleCsFileGenerator;

/// <summary>
/// MSBuild task that merges multiple C# source files into a single file.
/// </summary>
public partial class GenerateCSharpSingleFileTask : Microsoft.Build.Utilities.Task
{
    #region Nested types

    internal class SourceContext
    {
        public HashSet<string> Usings { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Namespaces { get; } = new(StringComparer.Ordinal);
        public List<(string Path, List<string> BodyLines)> SourceFiles { get; } = [];

        public HashSet<string> FilterInternalUsings()
        {
            HashSet<string> filteredUsings = new(StringComparer.Ordinal);

            foreach (string usingDirective in Usings)
            {
                bool isInternal = false;
                foreach (string ns in Namespaces)
                {
                    // Check if using references this namespace or a sub-namespace.
                    if ((usingDirective.IndexOf($"using {ns};") >= 0)
                        || (usingDirective.IndexOf($"using {ns}.") >= 0))
                    {
                        isInternal = true;
                        break;
                    }
                }

                if (!isInternal)
                {
                    filteredUsings.Add(usingDirective);
                }
            }

            return filteredUsings;
        }
    }

    #endregion Nested types

    private const string ThisProjectPackageName = "Kros.SingleCsFileGenerator";
    private const string DefaultSdk = "Microsoft.NET.Sdk";
    private const string ProgramCsFileName = "Program.cs";

    private static readonly Regex _namespaceDeclarationRegex = new(@"^\s*namespace\s+(?<namespace>[\w.]+)\s*;\s*$", RegexOptions.Compiled);
    private static readonly Regex _usingDeclarationRegex = new(@"^\s*(global\s+)?using\s+(?<using>[\w.]+)\s*;\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Name of the project which is converted to single file.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// The source C# files to merge.
    /// </summary>
    [Required]
    public ITaskItem[] SourceFiles { get; set; } = [];

    /// <summary>
    /// The output file path for the merged C# file.
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>
    /// The SDK used by the project (e.g., Microsoft.NET.Sdk, Microsoft.NET.Sdk.Web). Default is Microsoft.NET.Sdk.
    /// </summary>
    public string ProjectSdk { get; set; } = DefaultSdk;

    /// <summary>
    /// The package references from the project.
    /// </summary>
    public ITaskItem[] PackageReferences { get; set; } = [];

    /// <summary>
    /// The root namespace of the project (used to filter internal usings).
    /// </summary>
    public string RootNamespace { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            List<ITaskItem> sortedSourceFiles = SortSourceFiles();
            SourceContext context = LoadSourceFiles(sortedSourceFiles);
            List<string> output = BuildOutput(context);
            SaveOutput(output);
            Log.LogMessage(MessageImportance.High, $"Created merged C# file: {OutputFile}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private static string GetTaskItemFilePath(ITaskItem item)
    {
        string? path = item.GetMetadata("FullPath");
        return string.IsNullOrEmpty(path) ? item.ItemSpec : path;
    }

    private static string GetTaskItemFileName(ITaskItem item)
        => Path.GetFileName(GetTaskItemFilePath(item));

    private List<ITaskItem> SortSourceFiles()
        => SourceFiles
            .OrderBy(s => ProgramCsFileName.Equals(GetTaskItemFileName(s), StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

    private SourceContext LoadSourceFiles(IEnumerable<ITaskItem> sourceFiles)
    {
        SourceContext context = new();
        foreach (ITaskItem sourceFile in sourceFiles)
        {
            string path = GetTaskItemFilePath(sourceFile);
            if (!File.Exists(path))
            {
                Log.LogWarning($"Source file not found: {path}");
                continue;
            }

            List<string> bodyLines = [];

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                Match usingMatch = _usingDeclarationRegex.Match(line);
                if (usingMatch.Success)
                {
                    // Normalize using directive so it does not contain extra spaces.
                    context.Usings.Add($"using {usingMatch.Groups["using"].Value};");
                    continue;
                }
                else
                {
                    Match namespaceMatch = _namespaceDeclarationRegex.Match(line);
                    if (namespaceMatch.Success)
                    {
                        context.Namespaces.Add(namespaceMatch.Groups["namespace"].Value);
                    }
                    else
                    {
                        bodyLines.Add(line);
                    }
                }
            }

            // Remove empty lines at the begining and the end.
            int firstNonEmptyLineIndex = 0;
            foreach (string line in bodyLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
                firstNonEmptyLineIndex++;
            }
            int lastNonEmptyLineIndex = bodyLines.Count - 1;
            for (int i = bodyLines.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(bodyLines[i]))
                {
                    break;
                }
                lastNonEmptyLineIndex--;
            }
            context.SourceFiles.Add((path,
                bodyLines.GetRange(firstNonEmptyLineIndex, lastNonEmptyLineIndex - firstNonEmptyLineIndex + 1)));
        }

        context.FilterInternalUsings();
        return context;
    }

    private void SaveOutput(IEnumerable<string> output)
    {
        string? outputFolder = Path.GetDirectoryName(OutputFile);
        if (!string.IsNullOrWhiteSpace(outputFolder) && !Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        File.WriteAllLines(OutputFile, output);
    }

    private List<string> BuildOutput(SourceContext context)
    {
        List<string> output = [];
        AddSdkDirective(output);
        AddProperties(output);
        AddPackageDirectives(output);
        AddGeneratedInfo(output);
        AddUsings(output, context);
        AddSourceCode(output, context);
        return output;
    }

    private void AddSdkDirective(List<string> output)
    {
        if (!string.IsNullOrWhiteSpace(ProjectSdk) && !DefaultSdk.Equals(ProjectSdk))
        {
            output.Add($"#:sdk {ProjectSdk}");
            output.Add(string.Empty);
        }
    }

    private void AddProperties(List<string> output)
    {
        output.Add($"#:property PublishTrimmed=false");
        output.Add(string.Empty);
    }

    private void AddPackageDirectives(List<string> output)
    {
        bool added = false;
        foreach (ITaskItem package in PackageReferences)
        {
            string packageName = package.ItemSpec;

            // Skip this project package.
            if (packageName.Equals(ThisProjectPackageName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string version = package.GetMetadata("Version");
            if (!string.IsNullOrEmpty(version))
            {
                output.Add($"#:package {packageName}@{version}");
            }
            else
            {
                output.Add($"#:package {packageName}");
            }
            added = true;
        }
        if (added)
        {
            output.Add(string.Empty);
        }
    }

    private void AddGeneratedInfo(List<string> output)
    {
        output.Add($"// Auto-generated single-file .NET application for project {ProjectName}.");
        output.Add(string.Empty);
    }

    private static void AddUsings(List<string> output, SourceContext context)
    {
        HashSet<string> filteredUsings = context.FilterInternalUsings();

        foreach (string? usingDirective in filteredUsings.OrderBy(u => u, StringComparer.Ordinal))
        {
            output.Add(usingDirective);
        }
        output.Add(string.Empty);
    }

    private static void AddSourceCode(List<string> output, SourceContext context)
    {
        foreach ((string path, List<string> lines) in context.SourceFiles)
        {
            output.Add($"// {path}");
            output.Add(string.Empty);
            output.AddRange(lines);
            output.Add(string.Empty);
        }
    }
}
