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

    [KernelFunction]
    [Description("Applies a code edit to a file by searching for a block of code and replacing it with new code. Uses fuzzy matching to locate the search block.")]
    public string ApplyCodeEdit(
            [Description("The path of the file to edit, relative to the base path")] string filePath,
            [Description("The block of code to search for in the original content")] string searchBlock,
            [Description("The block of code to replace the search block with")] string replaceBlock)
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

            var originalFileContent = File.ReadAllText(fullPath);

            // Normalize line endings to avoid CRLF vs LF issues
            originalFileContent = NormalizeLineEndings(originalFileContent);
            searchBlock = NormalizeLineEndings(searchBlock);
            replaceBlock = NormalizeLineEndings(replaceBlock);

            // Validate search block is not empty
            if (string.IsNullOrWhiteSpace(searchBlock))
            {
                return "ERROR: Search block cannot be empty.";
            }

            // 1. Attempt Exact Match (Fastest)
            if (originalFileContent.Contains(searchBlock))
            {
                var newText = ReplaceFirstOccurrence(originalFileContent, searchBlock, replaceBlock);

                File.WriteAllText(fullPath, newText);

                return newText;
            }

            // 2. Attempt Whitespace-Insensitive Line Match (Good for indentation errors)
            var fileLines = SplitLines(originalFileContent);
            var searchLines = SplitLines(searchBlock);
            var replaceLines = SplitLines(replaceBlock);

            // Remove empty leading/trailing lines from search block (common LLM artifact)
            searchLines = TrimEmptyEnds(searchLines);

            int matchIndex = FindBlockIndex(fileLines, searchLines, fuzzyThreshold: 1.0); // 1.0 = strict text, loose whitespace

            // 3. Attempt Levenshtein Fuzzy Match (Good for typos/hallucinated comments)
            if (matchIndex == -1)
            {
                // Threshold 0.85 means 85% similarity required
                matchIndex = FindBlockIndex(fileLines, searchLines, fuzzyThreshold: 0.85);
            }

            if (matchIndex != -1)
            {
                var newText = ApplyLineReplacement(fileLines, searchLines, replaceLines, matchIndex);

                File.WriteAllText(fullPath, newText);

                return newText;
            }

            return "ERROR: Could not locate the SEARCH block in the file. Please ensure the SEARCH block matches the file content exactly.";
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to apply edit. {ex.Message}";
        }
    }

    // --- Helper Logic ---

    private int FindBlockIndex(List<string> fileLines, List<string> searchLines, double fuzzyThreshold)
    {
        if (searchLines.Count == 0) return -1;

        // Iterate through the file looking for the sequence
        for (int i = 0; i <= fileLines.Count - searchLines.Count; i++)
        {
            bool match = true;
            double totalScore = 0;

            for (int j = 0; j < searchLines.Count; j++)
            {
                string fileLine = fileLines[i + j].Trim();
                string searchLine = searchLines[j].Trim();

                // Skip comparison if both lines are empty (allows flexible blank line matching)
                if (string.IsNullOrWhiteSpace(fileLine) && string.IsNullOrWhiteSpace(searchLine))
                {
                    totalScore += 1.0;
                    continue;
                }

                if (fuzzyThreshold >= 1.0)
                {
                    // Strict whitespace-normalized equality
                    if (fileLine != searchLine)
                    {
                        match = false;
                        break;
                    }
                    totalScore += 1.0;
                }
                else
                {
                    // Levenshtein similarity
                    double score = CalculateSimilarity(fileLine, searchLine);
                    if (score < 0.6) // Hard cutoff for very different lines
                    {
                        match = false;
                        break;
                    }
                    totalScore += score;
                }
            }

            if (match)
            {
                double avgScore = totalScore / searchLines.Count;
                if (avgScore >= fuzzyThreshold) return i;
            }
        }
        return -1;
    }

    private string ApplyLineReplacement(List<string> fileLines, List<string> searchLines, List<string> replaceLines, int startIndex)
    {
        // Remove the old lines
        fileLines.RemoveRange(startIndex, searchLines.Count);
        // Insert the new lines
        fileLines.InsertRange(startIndex, replaceLines);
        return string.Join("\n", fileLines);
    }

    // --- Utility Functions ---

    private string ReplaceFirstOccurrence(string source, string search, string replace)
    {
        int index = source.IndexOf(search);
        if (index < 0) return source;
        return source.Remove(index, search.Length).Insert(index, replace);
    }

    private string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private List<string> SplitLines(string text)
    {
        return text.Split('\n').ToList();
    }

    private List<string> TrimEmptyEnds(List<string> lines)
    {
        var result = new List<string>(lines);
        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[0])) result.RemoveAt(0);
        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[result.Count - 1])) result.RemoveAt(result.Count - 1);
        return result;
    }

    // --- Levenshtein Implementation (Standard 0-1 Similarity) ---
    private static double CalculateSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        int distance = ComputeLevenshteinDistance(s1, s2);
        return 1.0 - (double)distance / Math.Max(s1.Length, s2.Length);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
