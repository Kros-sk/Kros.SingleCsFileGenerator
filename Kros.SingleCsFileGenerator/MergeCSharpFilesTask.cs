using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Kros.SingleCsFileGenerator;

/// <summary>
/// MSBuild task that merges multiple C# source files into a single amalgamated file.
/// </summary>
public class MergeCSharpFilesTask : Microsoft.Build.Utilities.Task
{
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

    public override bool Execute()
    {
        try
        {
            var usings = new HashSet<string>(StringComparer.Ordinal);
            var bodyLines = new List<string>();
            var sourcePaths = new List<string>();

            foreach (var source in Sources)
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

                sourcePaths.Add(path);
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
                    else
                    {
                        bodyLines.Add(line);
                    }
                }
            }

            // Build output content
            var outputLines = new List<string>
            {
                "// Auto-generated single-file amalgamation",
                "// Sources:"
            };

            foreach (var sourcePath in sourcePaths)
            {
                outputLines.Add($"// - {sourcePath}");
            }

            outputLines.Add(string.Empty);

            // Add sorted usings
            foreach (var usingDirective in usings.OrderBy(u => u, StringComparer.Ordinal))
            {
                outputLines.Add(usingDirective);
            }

            outputLines.Add(string.Empty);

            // Add body lines
            outputLines.AddRange(bodyLines);

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
}

