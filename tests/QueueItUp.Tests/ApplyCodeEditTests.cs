using QueueItUp.Agent.Plugins;

namespace QueueItUp.Tests;

/// <summary>
/// Comprehensive tests for FileSystemPlugin.ApplyCodeEdit method.
/// Tests directly call the method without LLM involvement.
/// </summary>
public class ApplyCodeEditTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly FileSystemPlugin _plugin;

    public ApplyCodeEditTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), "QueueItUp.Tests", "ApplyCodeEdit", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBasePath);
        _plugin = new FileSystemPlugin(_testBasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, true);
        }
    }

    #region Exact Match Tests

    [Fact]
    public void ApplyCodeEdit_ExactMatch_ShouldReplaceCode()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public class Test
{
    public void Method1()
    {
        Console.WriteLine(""Hello"");
    }
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"    public void Method1()
    {
        Console.WriteLine(""Hello"");
    }";
        var replaceBlock = @"    public void Method1()
    {
        Console.WriteLine(""Hello, World!"");
    }";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains(@"Console.WriteLine(""Hello, World!"")", result);
        var fileContent = File.ReadAllText(Path.Combine(_testBasePath, fileName));
        Assert.Contains(@"Console.WriteLine(""Hello, World!"")", fileContent);
    }

    [Fact]
    public void ApplyCodeEdit_ExactMatch_SingleLine_ShouldWork()
    {
        // Arrange
        var fileName = "test.txt";
        var originalContent = "Line 1\nLine 2\nLine 3";
        CreateTestFile(fileName, originalContent);

        var searchBlock = "Line 2";
        var replaceBlock = "Modified Line 2";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Modified Line 2", result);
    }

    #endregion

    #region Line Ending Tests

    [Fact]
    public void ApplyCodeEdit_DifferentLineEndings_ShouldNormalize()
    {
        // Arrange
        var fileName = "test.cs";
        // File has CRLF
        var originalContent = "Line 1\r\nLine 2\r\nLine 3";
        CreateTestFile(fileName, originalContent);

        // Search block has LF
        var searchBlock = "Line 1\nLine 2";
        var replaceBlock = "Modified Line 1\nModified Line 2";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Modified Line 1", result);
        Assert.Contains("Modified Line 2", result);
    }

    [Fact]
    public void ApplyCodeEdit_MacOSLineEndings_ShouldNormalize()
    {
        // Arrange
        var fileName = "test.txt";
        // File has old Mac line endings (CR)
        var originalContent = "Line 1\rLine 2\rLine 3";
        CreateTestFile(fileName, originalContent);

        var searchBlock = "Line 2";
        var replaceBlock = "Modified Line 2";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Modified Line 2", result);
    }

    #endregion

    #region Whitespace Tests

    [Fact]
    public void ApplyCodeEdit_DifferentIndentation_ShouldMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public class Test
{
    public void Method1()
    {
        Console.WriteLine(""Hello"");
    }
}";
        CreateTestFile(fileName, originalContent);

        // Search block has different indentation (2 spaces instead of 4)
        var searchBlock = @"  public void Method1()
  {
    Console.WriteLine(""Hello"");
  }";
        var replaceBlock = @"    public void Method1()
    {
        Console.WriteLine(""Hello, World!"");
    }";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains(@"Console.WriteLine(""Hello, World!"")", result);
    }

    [Fact]
    public void ApplyCodeEdit_ExtraWhitespace_ShouldMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = "public void Method1()\n{\n    Console.WriteLine(\"Hello\");\n}";
        CreateTestFile(fileName, originalContent);

        // Search block has extra spaces at end of lines
        var searchBlock = "public void Method1()  \n{\n    Console.WriteLine(\"Hello\");  \n}";
        var replaceBlock = "public void Method1()\n{\n    Console.WriteLine(\"Hello, World!\");\n}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Hello, World!", result);
    }

    #endregion

    #region Empty Line Tests

    [Fact]
    public void ApplyCodeEdit_EmptyLinesInSearchBlock_ShouldTrimAndMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{
    Console.WriteLine(""Hello"");
}";
        CreateTestFile(fileName, originalContent);

        // Search block has empty lines at start and end
        var searchBlock = @"

public void Method1()
{
    Console.WriteLine(""Hello"");
}

";
        var replaceBlock = @"public void Method1()
{
    Console.WriteLine(""Hello, World!"");
}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Hello, World!", result);
    }

    [Fact]
    public void ApplyCodeEdit_BlankLinesInMiddle_ShouldMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{

    Console.WriteLine(""Hello"");

}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"public void Method1()
{

    Console.WriteLine(""Hello"");

}";
        var replaceBlock = @"public void Method1()
{
    Console.WriteLine(""Hello, World!"");
}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Hello, World!", result);
    }

    #endregion

    #region Fuzzy Matching Tests

    [Fact]
    public void ApplyCodeEdit_MinorTypo_ShouldMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{
    Console.WriteLine(""Hello"");
}";
        CreateTestFile(fileName, originalContent);

        // Search block has a minor typo (WriteLine vs WriteLin)
        var searchBlock = @"public void Method1()
{
    Console.WriteLin(""Hello"");
}";
        var replaceBlock = @"public void Method1()
{
    Console.WriteLine(""Hello, World!"");
}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        // Should match because of fuzzy matching (85% similarity threshold)
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Hello, World!", result);
    }

    [Fact]
    public void ApplyCodeEdit_CommentDifference_ShouldMatch()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{
    // This is a comment
    Console.WriteLine(""Hello"");
}";
        CreateTestFile(fileName, originalContent);

        // Search block has slightly different comment
        var searchBlock = @"public void Method1()
{
    // This is comment
    Console.WriteLine(""Hello"");
}";
        var replaceBlock = @"public void Method1()
{
    // Updated comment
    Console.WriteLine(""Hello, World!"");
}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Updated comment", result);
    }

    #endregion

    #region Multiple Occurrences Tests

    [Fact]
    public void ApplyCodeEdit_MultipleOccurrences_ShouldReplaceFirstOnly()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{
    Console.WriteLine(""Hello"");
}

public void Method2()
{
    Console.WriteLine(""Hello"");
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"Console.WriteLine(""Hello"");";
        var replaceBlock = @"Console.WriteLine(""Goodbye"");";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        // First occurrence should be replaced
        Assert.Contains("Goodbye", result);
        // Should still have at least one "Hello" (from second method)
        var occurrences = CountOccurrences(result, "Hello");
        Assert.Equal(1, occurrences); // Second occurrence should remain
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ApplyCodeEdit_EmptySearchBlock_ShouldReturnError()
    {
        // Arrange
        var fileName = "test.cs";
        CreateTestFile(fileName, "Some content");

        var searchBlock = "";
        var replaceBlock = "Replacement";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.Contains("ERROR", result);
    }

    [Fact]
    public void ApplyCodeEdit_SearchBlockNotFound_ShouldReturnError()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public void Method1()
{
    Console.WriteLine(""Hello"");
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"public void NonExistentMethod()
{
    Console.WriteLine(""Goodbye"");
}";
        var replaceBlock = @"public void ReplacedMethod()
{
}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.Contains("ERROR: Could not locate the SEARCH block", result);
    }

    [Fact]
    public void ApplyCodeEdit_FileNotFound_ShouldReturnError()
    {
        // Arrange
        var fileName = "nonexistent.cs";
        var searchBlock = "search";
        var replaceBlock = "replace";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.Contains("Error: File not found", result);
    }

    [Fact]
    public void ApplyCodeEdit_PathTraversal_ShouldDenyAccess()
    {
        // Arrange
        var maliciousPath = "../../../etc/passwd";
        var searchBlock = "root";
        var replaceBlock = "hacker";

        // Act
        var result = _plugin.ApplyCodeEdit(maliciousPath, searchBlock, replaceBlock);

        // Assert
        Assert.Contains("Error: Access denied", result);
    }

    [Fact]
    public void ApplyCodeEdit_EntireFileReplacement_ShouldWork()
    {
        // Arrange
        var fileName = "test.txt";
        var originalContent = "Old content";
        CreateTestFile(fileName, originalContent);

        var searchBlock = "Old content";
        var replaceBlock = "New content";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("New content", result);
        Assert.DoesNotContain("Old content", result);
    }

    #endregion

    #region Complex Code Tests

    [Fact]
    public void ApplyCodeEdit_ComplexMethod_ShouldWork()
    {
        // Arrange
        var fileName = "complex.cs";
        var originalContent = @"public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"    public int Subtract(int a, int b)
    {
        return a - b;
    }";
        var replaceBlock = @"    public int Subtract(int a, int b)
    {
        // Improved subtraction with validation
        if (b > a)
            throw new ArgumentException(""Cannot subtract larger from smaller"");
        return a - b;
    }";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Improved subtraction with validation", result);
        Assert.Contains("throw new ArgumentException", result);
        // Other methods should remain unchanged
        Assert.Contains("public int Add", result);
        Assert.Contains("public int Multiply", result);
    }

    [Fact]
    public void ApplyCodeEdit_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var fileName = "special.cs";
        var originalContent = @"public string GetMessage()
{
    return ""Hello\nWorld\t!"";
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"    return ""Hello\nWorld\t!"";";
        var replaceBlock = @"    return ""Goodbye\nUniverse\t!"";";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Goodbye", result);
        Assert.Contains("Universe", result);
    }

    [Fact]
    public void ApplyCodeEdit_MultilineString_ShouldWork()
    {
        // Arrange
        var fileName = "multiline.cs";
        var originalContent = @"public string GetTemplate()
{
    return @""Line 1
Line 2
Line 3"";
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = @"    return @""Line 1
Line 2
Line 3"";";
        var replaceBlock = @"    return @""Line 1
Modified Line 2
Line 3"";";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Modified Line 2", result);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void ApplyCodeEdit_ReplaceFirstLine_ShouldWork()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"using System;
using System.Collections.Generic;

public class Test { }";
        CreateTestFile(fileName, originalContent);

        var searchBlock = "using System;";
        var replaceBlock = "using System;\nusing System.Linq;";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("using System.Linq;", result);
    }

    [Fact]
    public void ApplyCodeEdit_ReplaceLastLine_ShouldWork()
    {
        // Arrange
        var fileName = "test.cs";
        var originalContent = @"public class Test
{
    public void Method() { }
}";
        CreateTestFile(fileName, originalContent);

        var searchBlock = "}";
        var replaceBlock = "    // End of class\n}";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("// End of class", result);
    }

    [Fact]
    public void ApplyCodeEdit_SingleCharacterFile_ShouldWork()
    {
        // Arrange
        var fileName = "tiny.txt";
        CreateTestFile(fileName, "A");

        var searchBlock = "A";
        var replaceBlock = "B";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Equal("B", result);
    }

    [Fact]
    public void ApplyCodeEdit_VeryLongFile_ShouldWork()
    {
        // Arrange
        var fileName = "long.txt";
        var lines = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            lines.Add($"Line {i}");
        }
        var originalContent = string.Join("\n", lines);
        CreateTestFile(fileName, originalContent);

        var searchBlock = "Line 500\nLine 501\nLine 502";
        var replaceBlock = "Modified Line 500\nModified Line 501\nModified Line 502";

        // Act
        var result = _plugin.ApplyCodeEdit(fileName, searchBlock, replaceBlock);

        // Assert
        Assert.DoesNotContain("ERROR", result);
        Assert.Contains("Modified Line 500", result);
        Assert.Contains("Modified Line 501", result);
        Assert.Contains("Modified Line 502", result);
    }

    #endregion

    #region Helper Methods

    private void CreateTestFile(string fileName, string content)
    {
        var fullPath = Path.Combine(_testBasePath, fileName);
        File.WriteAllText(fullPath, content);
    }

    private int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    #endregion
}
