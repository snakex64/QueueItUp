using QueueItUp.Agent.Plugins;

namespace QueueItUp.Tests;

public class FileSystemPluginTests
{
    private readonly string _testBasePath;

    public FileSystemPluginTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "QueueItUp.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);
    }

    [Fact]
    public void FileSystemPlugin_Constructor_WithValidPath_ShouldSucceed()
    {
        // Act & Assert
        var plugin = new FileSystemPlugin(_testBasePath);
        Assert.NotNull(plugin);
    }

    [Fact]
    public void FileSystemPlugin_Constructor_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var invalidPath = Path.Combine(_testBasePath, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => new FileSystemPlugin(invalidPath));
    }

    // UpdateFile method was replaced with ApplyCodeEdit
    // See ApplyCodeEditTests.cs for comprehensive tests

    [Fact]
    public async Task FileSystemPlugin_ReadFile_ExistingFile_ShouldReturnContent()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);
        var fileName = "test.txt";
        var content = "Hello, World!";
        var fullPath = Path.Combine(_testBasePath, fileName);
        await File.WriteAllTextAsync(fullPath, content);

        // Act
        var result = await plugin.ReadFile(fileName);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public async Task FileSystemPlugin_ReadFile_NonExistingFile_ShouldReturnError()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);
        var fileName = "nonexistent.txt";

        // Act
        var result = await plugin.ReadFile(fileName);

        // Assert
        Assert.Contains("Error: File not found", result);
    }

    [Fact]
    public void FileSystemPlugin_ListFiles_SimplePattern_ShouldReturnMatchingFiles()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);
        var file1 = Path.Combine(_testBasePath, "test1.txt");
        var file2 = Path.Combine(_testBasePath, "test2.txt");
        var file3 = Path.Combine(_testBasePath, "test.cs");
        File.WriteAllText(file1, "content1");
        File.WriteAllText(file2, "content2");
        File.WriteAllText(file3, "content3");

        // Act
        var result = plugin.ListFiles("*.txt");

        // Assert
        Assert.Contains("test1.txt", result);
        Assert.Contains("test2.txt", result);
        Assert.DoesNotContain("test.cs", result);
    }

    [Fact]
    public void FileSystemPlugin_ListFiles_NoMatchingFiles_ShouldReturnMessage()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);

        // Act
        var result = plugin.ListFiles("*.txt");

        // Assert
        Assert.Contains("No files found matching pattern", result);
    }

    [Fact]
    public async Task FileSystemPlugin_ReadFile_PathTraversal_ShouldDenyAccess()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);
        var maliciousPath = "../../../etc/passwd";

        // Act
        var result = await plugin.ReadFile(maliciousPath);

        // Assert
        Assert.Contains("Error: Access denied - path is outside the allowed directory", result);
    }

    // Path traversal test for ApplyCodeEdit is in ApplyCodeEditTests.cs

    [Fact]
    public void FileSystemPlugin_ListFiles_PathTraversal_ShouldDenyAccess()
    {
        // Arrange
        var plugin = new FileSystemPlugin(_testBasePath);
        var maliciousPattern = "../../../*.txt";

        // Act
        var result = plugin.ListFiles(maliciousPattern);

        // Assert
        Assert.Contains("Error: Pattern specifies a directory outside the allowed base path", result);
    }
}
