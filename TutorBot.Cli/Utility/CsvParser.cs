using System.Text;

namespace TutorBot.Utility;

public static class CsvParser
{
  public static async IAsyncEnumerable<List<string>> Parse(Stream stream, char separator = ',')
  {
    using (var reader = new StreamReader(stream))
    {
      while (!reader.EndOfStream)
      {
        var line = await reader.ReadLineAsync();
        if (!string.IsNullOrEmpty(line))
        {
          yield return ParseLine(line, separator);
        }
      }
    }
  }

  private static List<string> ParseLine(string line, char separator)
  {
    var values = new List<string>();

    var quotedValue = false;
    var currentField = new StringBuilder();

    for (int i = 0; i < line.Length; i++)
    {
      char c = line[i];

      if (c == '\"')
      {
        quotedValue = !quotedValue;
      }
      else if (c == separator && !quotedValue)
      {
        values.Add(currentField.ToString());
        currentField.Clear();
      }
      else
      {
        currentField.Append(c);
      }
    }

    values.Add(currentField.ToString());

    return values;
  }
}

