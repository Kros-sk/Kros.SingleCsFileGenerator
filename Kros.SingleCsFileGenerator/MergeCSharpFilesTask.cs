using Microsoft.Build.Framework;
using System.Text.RegularExpressions;

namespace Kros.SingleCsFileGenerator;

/// <summary>
/// MSBuild task that merges multiple C# source files into a single file.
/// </summary>
public class MergeCSharpFilesTask : Microsoft.Build.Utilities.Task
{
    private static readonly Regex NamespaceDeclarationRegex = new(
        @"^\s*namespace\s+[\w.]+\s*;\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// The source C# files to merge.
    /// </summary>
    [Required]
    public ITaskItem[] Sources { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// The output file path for the merged C# file.
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>
    /// The SDK used by the project (e.g., Microsoft.NET.Sdk.Web).
    /// </summary>
    public string ProjectSdk { get; set; } = "Microsoft.NET.Sdk";

    /// <summary>
    /// The package references from the project.
    /// </summary>
    public ITaskItem[] PackageReferences { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// The root namespace of the project (used to filter internal usings).
    /// </summary>
    public string RootNamespace { get; set; } = string.Empty;

    public override bool Execute()
    {
        try
        {
            var usings = new HashSet<string>(StringComparer.Ordinal);
            var fileContents = new List<(string Path, List<string> BodyLines)>();

            // Sort sources so Program.cs is processed last
            var sortedSources = Sources
                .OrderBy(s => Path.GetFileName(s.GetMetadata("FullPath") ?? s.ItemSpec)
                    .Equals("Program.cs", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ToList();

            foreach (var source in sortedSources)
            {
                var path = source.GetMetadata("FullPath");
                if (string.IsNullOrEmpty(path))
                {
                    path = source.ItemSpec;
                }

                if (!File.Exists(path))
                {
                    Log.LogWarning($"Source file not found: {path}");
                    continue;
                }

                var bodyLines = new List<string>();
                var lines = File.ReadAllLines(path);

                foreach (var line in lines)
                {
                    var trimmedStart = line.TrimStart();
                    var trimmedEnd = line.TrimEnd();

                    // Handle regular and global using directives
                    if ((trimmedStart.StartsWith("using ") || trimmedStart.StartsWith("global using "))
                        && trimmedEnd.EndsWith(";"))
                    {
                        usings.Add(line.Trim());
                    }
                    // Skip namespace declarations (file-scoped namespaces)
                    else if (NamespaceDeclarationRegex.IsMatch(line))
                    {
                        // Skip namespace declaration
                        continue;
                    }
                    else
                    {
                        bodyLines.Add(line);
                    }
                }

                fileContents.Add((path, bodyLines));
            }

            // Filter out internal usings (those referencing project namespaces)
            var filteredUsings = FilterInternalUsings(usings);

            // Build output content
            var outputLines = new List<string>();

            // Add SDK directive
            if (!string.IsNullOrEmpty(ProjectSdk) && ProjectSdk != "Microsoft.NET.Sdk")
            {
                outputLines.Add($"#:sdk {ProjectSdk}");
            }

            // Add package directives
            foreach (var package in PackageReferences)
            {
                var packageName = package.ItemSpec;

                // Skip Kros.SingleCsFileGenerator
                if (packageName.Equals("Kros.SingleCsFileGenerator", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var version = package.GetMetadata("Version");
                if (!string.IsNullOrEmpty(version))
                {
                    outputLines.Add($"#:package {packageName}@{version}");
                }
                else
                {
                    outputLines.Add($"#:package {packageName}");
                }
            }

            if (outputLines.Count > 0)
            {
                outputLines.Add(string.Empty);
            }

            // Add header comments
            outputLines.Add("// Auto-generated single-file.");
            outputLines.Add("// Sources:");

            foreach (var (path, _) in fileContents)
            {
                outputLines.Add($"// - {path}");
            }

            outputLines.Add(string.Empty);

            // Add sorted usings (filtered)
            foreach (var usingDirective in filteredUsings.OrderBy(u => u, StringComparer.Ordinal))
            {
                outputLines.Add(usingDirective);
            }

            outputLines.Add(string.Empty);

            // Add body lines from all files
            foreach (var (_, bodyLines) in fileContents)
            {
                outputLines.AddRange(bodyLines);
            }

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(OutputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Write output file
            File.WriteAllLines(OutputFile, outputLines);

            Log.LogMessage(MessageImportance.High, $"Created merged C# file: {OutputFile}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private HashSet<string> FilterInternalUsings(HashSet<string> usings)
    {
        var filtered = new HashSet<string>(StringComparer.Ordinal);

        // Collect all namespace patterns to filter out
        var internalNamespaces = new HashSet<string>(StringComparer.Ordinal);

        // If RootNamespace is provided, use it
        if (!string.IsNullOrEmpty(RootNamespace))
        {
            internalNamespaces.Add(RootNamespace);
        }

        // Also detect namespaces from the source files themselves
        foreach (var source in Sources)
        {
            var path = source.GetMetadata("FullPath");
            if (string.IsNullOrEmpty(path))
            {
                path = source.ItemSpec;
            }

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var match = NamespaceDeclarationRegex.Match(line);
                    if (match.Success)
                    {
                        // Extract namespace from "namespace X.Y.Z;"
                        var ns = line.Trim()
                            .Replace("namespace ", "")
                            .Replace(";", "")
                            .Trim();
                        internalNamespaces.Add(ns);
                    }
                }
            }
        }

        foreach (var usingDirective in usings)
        {
            var isInternal = false;

            foreach (var ns in internalNamespaces)
            {
                // Check if using references this namespace or a sub-namespace
                // e.g., "using Kros.SingleCsFileGenerator.Demo.DTOs;"
                if (usingDirective.Contains($"using {ns}") ||
                    usingDirective.Contains($"using {ns}."))
                {
                    isInternal = true;
                    break;
                }
            }

            if (!isInternal)
            {
                filtered.Add(usingDirective);
            }
        }

        return filtered;
    }
}
