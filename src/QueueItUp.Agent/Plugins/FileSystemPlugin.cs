using DiffMatchPatch;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace QueueItUp.Agent.Plugins;

/// <summary>
/// Plugin that provides file system operations for agents.
/// Allows listing, reading, and updating files.
/// </summary>
public class FileSystemPlugin
{
    private readonly string _basePath;

    public FileSystemPlugin(string basePath)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        
        if (!Directory.Exists(_basePath))
        {
            throw new DirectoryNotFoundException($"Base path does not exist: {_basePath}");
        }
    }

    /// <summary>
    /// Lists files matching the specified pattern (e.g., "src/*.cs", "**/*.txt").
    /// </summary>
    [KernelFunction, Description("Lists files matching a glob pattern like 'src/*.cs' or '**/*.txt'")]
    public string ListFiles(
        [Description("The glob pattern to match files, e.g., 'src/*.cs' or '**/*.txt'")] string pattern)
    {
        try
        {
            var files = new List<string>();
            var basePath = Path.GetFullPath(_basePath);
            
            // Handle different pattern types
            if (pattern.Contains("**"))
            {
                // Recursive pattern
                var parts = pattern.Split("**", 2);
                var dirPart = string.IsNullOrWhiteSpace(parts[0]) ? _basePath : Path.Combine(_basePath, parts[0].Trim('/').Trim('\\'));
                var searchPattern = parts.Length > 1 ? parts[1].Trim('/').Trim('\\') : "*";
                
                // Validate the directory part is within base path
                var fullDirPath = Path.GetFullPath(dirPart);
                if (!fullDirPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Error: Pattern specifies a directory outside the allowed base path";
                }
                
                if (Directory.Exists(fullDirPath))
                {
                    files.AddRange(Directory.GetFiles(fullDirPath, searchPattern, SearchOption.AllDirectories));
                }
            }
            else
            {
                // Simple pattern
                var directory = Path.GetDirectoryName(pattern);
                var fileName = Path.GetFileName(pattern);
                
                var searchDir = string.IsNullOrEmpty(directory) ? _basePath : Path.Combine(_basePath, directory);
                var searchPattern = string.IsNullOrEmpty(fileName) ? "*" : fileName;
                
                // Validate the search directory is within base path
                var fullSearchDir = Path.GetFullPath(searchDir);
                if (!fullSearchDir.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Error: Pattern specifies a directory outside the allowed base path";
                }
                
                if (Directory.Exists(fullSearchDir))
                {
                    files.AddRange(Directory.GetFiles(fullSearchDir, searchPattern, SearchOption.AllDirectories));
                }
            }

            // Validate all returned files are within base path and make paths relative
            var relativeFiles = new List<string>();
            foreach (var file in files)
            {
                var fullPath = Path.GetFullPath(file);
                if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    relativeFiles.Add(Path.GetRelativePath(_basePath, fullPath));
                }
            }
            
            if (relativeFiles.Count == 0)
            {
                return $"No files found matching pattern: {pattern}";
            }

            return string.Join("\n", relativeFiles);
        }
        catch (Exception ex)
        {
            return $"Error listing files: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads the content of a file at the specified path.
    /// </summary>
    [KernelFunction, Description("Reads the content of a file at the specified path. Do not modify the path, leave it exactly as received from ListFiles plugin")]
    public async Task<string> ReadFile(
        [Description("The full path of the file to read, relative to the base path")] string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(_basePath, filePath));
            var basePath = Path.GetFullPath(_basePath);
            
            // Validate that the resolved path is within the base directory
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Access denied - path is outside the allowed directory: {filePath}";
            }
            
            if (!File.Exists(fullPath))
            {
                return $"Error: File not found at path: {filePath}";
            }

            var content = await File.ReadAllTextAsync(fullPath);
            return content;
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates multiple ranges of lines in a file with the specified content for each range.
    /// </summary>
    [KernelFunction]
    [Description("Replaces code between two anchors using fuzzy matching. Best for inserting/changing code when you aren't 100% sure of the surrounding whitespace. i.e. to add new functions use the end of a method as 'startAnchor' and the beginning of the next one as 'endAnchor', this will insert the 'newContent' in between the two methods. Use short and concise anchors such as 2-3 lines. Do not use lines like '}' or '{' since those are found everywhere.")]
    public string UpdateCodeBetweenAnchorsFuzzy(
     [Description("Relative path to the file.")] string relativeFilePath,
     [Description("The previous couple of lines BEFORE the change. Must be significative enough that it can be found and matched in the file. DO NOT give the start of a method unless you're trying to replace that method. Give 3-4 lines.")] string startAnchor,
     [Description("The following couple of lines AFTER the change. Must be significative enough that it can be found and matched in the file. DO NOT give the end of a method unless you're trying to replace that method. DO NOT give simple anchors like '{' and '}'. Give 3-4 lines.")] string endAnchor,
     [Description("The new code to insert in between the anchors.")] string newContent)
    {
        try
        {
            var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativeFilePath));
            var basePath = Path.GetFullPath(_basePath);

            // Validate that the resolved path is within the base directory
            if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return $"Error: Access denied - path is outside the allowed directory: {relativeFilePath}";
            }
            string fileContent = File.ReadAllText(fullPath);

            // Normalize inputs strictly for the diff algorithm to function best
            // We do not modify the original fileContent yet.
            string contentForMatching = fileContent.Replace("\r\n", "\n");
            string startAnchorNorm = startAnchor.Replace("\r\n", "\n").Trim(); // Trim helps fuzzy match focus on content
            string endAnchorNorm = endAnchor.Replace("\r\n", "\n").Trim();

            var dmp = new diff_match_patch
            {
                Match_Threshold = 0.5f,   // 0.5 = loose match, 0.0 = exact
                Match_Distance = 5000    // Look far and wide in the file
            };

            // ---------------------------------------------------------
            // 1. Locate the START Anchor
            // ---------------------------------------------------------
            int startIndex = dmp.match_main(contentForMatching, startAnchorNorm, 0);

            if (startIndex == -1)
                return $"Error: Start anchor not found (Fuzzy match failed). Anchor: '{startAnchor.Substring(0, Math.Min(20, startAnchor.Length))}...'";

            // 'match_main' returns the index of the BEGINNING of the match.
            // We need the END of the match to insert *after* it.
            // Since it's fuzzy, we can't just add startAnchor.Length.
            // We assume the match length is roughly the anchor length, but let's verify.
            // Strategy: Use the found index, grab a substring of anchor length, and assume that's the spot.
            // A safer way in DMP is tricky, but adding length is standard for 'match'.
            int startInsertionPoint = startIndex + startAnchorNorm.Length;

            // ---------------------------------------------------------
            // 2. Locate the END Anchor
            // ---------------------------------------------------------
            // We search starting from where the first anchor ended to avoid finding an end anchor *before* the start.
            int searchFrom = startInsertionPoint;
            if (searchFrom >= contentForMatching.Length)
                return "Error: Start anchor is at the very end of the file.";

            int endIndex = dmp.match_main(contentForMatching, endAnchorNorm, searchFrom);

            if (endIndex == -1)
                return $"Error: End anchor not found after the start anchor. Anchor: '{endAnchor.Substring(0, Math.Min(20, endAnchor.Length))}...'";

            // The insertion point for the end anchor is its BEGINNING (we insert *before* it).
            int endInsertionPoint = endIndex;

            // ---------------------------------------------------------
            // 3. Validation
            // ---------------------------------------------------------
            if (endInsertionPoint <= startInsertionPoint)
            {
                // If the fuzzy matcher found the end anchor overlapping or before the start anchor
                return "Error: The End Anchor was found before or overlapping the Start Anchor. Please provide unique anchors.";
            }

            // ---------------------------------------------------------
            // 4. Stitching
            // ---------------------------------------------------------
            // Note: We used 'contentForMatching' (LF only) for indices.
            // If the original file was CRLF, indices might slightly drift if we aren't careful.
            // SAFEST BET: Reconstruct using the LF normalized string, then convert back to CRLF at the end.

            string partA = contentForMatching.Substring(0, startInsertionPoint);
            string partB = newContent.Replace("\r\n", "\n");
            string partC = contentForMatching.Substring(endInsertionPoint);

            // Does Part A end with a newline? If not, and Part B doesn't start with one, we might merge lines.
            // Optional: Smart whitespace injection
            if (!partA.EndsWith("\n") && !partB.StartsWith("\n")) partB = "\n" + partB;
            if (!partB.EndsWith("\n") && !partC.StartsWith("\n")) partB = partB + "\n";

            string finalContent = partA + partB + partC;

            // ---------------------------------------------------------
            // 5. Restore Windows Line Endings
            // ---------------------------------------------------------
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || fileContent.Contains("\r\n"))
            {
                finalContent = finalContent.Replace("\n", "\r\n");
            }

            File.WriteAllText(fullPath + ".bak", fileContent);
            File.WriteAllText(fullPath, finalContent);

            return "Success: Updated between fuzzy anchors.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
