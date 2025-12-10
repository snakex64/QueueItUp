using System.ComponentModel;
using Microsoft.SemanticKernel;

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
                    files.AddRange(Directory.GetFiles(fullSearchDir, searchPattern, SearchOption.TopDirectoryOnly));
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
    [KernelFunction, Description("Reads the content of a file at the specified path")]
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
    /// Updates or creates a file with the specified content.
    /// </summary>
    [KernelFunction, Description("Updates or creates a file with new content at the specified path")]
    public async Task<string> UpdateFile(
        [Description("The full path of the file to update, relative to the base path")] string filePath,
        [Description("The new content to write to the file")] string content)
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
            
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
            return $"Successfully updated file: {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error updating file: {ex.Message}";
        }
    }
}
