using System.Globalization;

namespace TutorBot.Infrastructure.StringExtensions;

public static class StringExtensions
{
  public static DateTime ToDateTime(this string value)
  {
    return DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
  }
}
