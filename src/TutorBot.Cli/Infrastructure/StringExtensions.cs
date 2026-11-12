using System.Globalization;
using System.Text;

namespace TutorBot.Infrastructure.StringExtensions;

public static class StringExtensions
{
  extension(string value)
  {
    public DateTime ToDateTime()
    {
      return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
    }

    public string Indent(int size)
    {
      var indentString = new string(' ', size);
      var lines = value.Split('\n');
      var sb = new StringBuilder();
      foreach (var line in lines)
        sb.Append(indentString).AppendLine(line);
      return sb.ToString();
    }

    public int WordCount()
    {
      int count = 0;
      bool inWord = false;

      foreach (char c in value)
      {
        if (Char.IsWhiteSpace(c))
        {
          // If the character is whitespace, we're not in a word anymore
          inWord = false;
        }
        else if (!inWord)
        {
          // If we're not in a word but we just encountered a non-whitespace character, 
          // then we're starting a new word
          inWord = true;
          count++;
        }
      }

      return count;
    }
  }
}
