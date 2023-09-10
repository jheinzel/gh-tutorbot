using System.Globalization;
using System.Text;

namespace TutorBot.Infrastructure.StringExtensions;

public static class StringExtensions
{
  public static DateTime ToDateTime(this string value)
  {
    return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
  }

  public static string Indent(this string value, int size)
  {
    var indentString = new string(' ', size);
    var lines = value.Split('\n');
    var sb = new StringBuilder();
    foreach (var line in lines)
      sb.Append(indentString).AppendLine(line);
    return sb.ToString();
  }
}
