using FluentAssertions;
using TutorBot.Utility;

namespace TutorBot.Tests;

public class CsvParserTests
{
  [Fact]
  public void ParseCsv_FirstLine_ShouldBeIgnored()
  {
    // Arrange
    var csvData = "Last,First\nAlice,Bob";
    using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    // Act
    var parsedCsvData = CsvParser.Parse(memoryStream, ignoreFirstLine: true).ToBlockingEnumerable();

    // Assert
    var expectedData = new List<List<string>>
    {
        new List<string> { "Alice", "Bob" }
    };

    parsedCsvData.Should().BeEquivalentTo(expectedData);
  }

  [Fact]
  public void ParseCsv_WithCommaSeparator_ShouldParseCorrectly()
  {
    // Arrange
    var csvData = "John,Doe\nAlice,Bob";
    using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    // Act
    var parsedCsvData = CsvParser.Parse(memoryStream).ToBlockingEnumerable();

    // Assert
    var expectedData = new List<List<string>>
    {
        new List<string> { "John", "Doe" },
        new List<string> { "Alice", "Bob" }
    };

    parsedCsvData.Should().BeEquivalentTo(expectedData);
  }

  [Fact]
  public void ParseCsv_WithSemicolonSeparator_ShouldParseCorrectly()
  {
    // Arrange
    var csvData = "Alice;Bob\nJohn;Doe";
    using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    char separator = ';';

    // Act
    var parsedCsvData = CsvParser.Parse(memoryStream, separator: separator).ToBlockingEnumerable();

    // Assert
    var expectedData = new List<List<string>>
    {
        new List<string> { "Alice", "Bob" },
        new List<string> { "John", "Doe" }
    };

    parsedCsvData.Should().BeEquivalentTo(expectedData);
  }

  [Fact]
  public void ParseCsv_WithQuotedItems_ShouldParseCorrectly()
  {
    // Arrange
    var csvData = "\"Alice \",\" Bob \"";
    using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));


    // Act
    var parsedCsvData = CsvParser.Parse(memoryStream).ToBlockingEnumerable();

    // Assert
    var expectedData = new List<List<string>>
    {
       new List<string> { "Alice ", " Bob " },
    };

    parsedCsvData.Should().BeEquivalentTo(expectedData);
  }
}

