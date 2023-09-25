namespace TutorBot.Infrastructure.CollectionExtensions;

public static class CollectionExtensions
{
  public static void Shuffle<T>(this IList<T> list)
  {
    var random = new Random();

    for (int i=list.Count-1; i > 0; i--)
    {
      int k = random.Next(i);
      (list[k], list[i]) = (list[i], list[k]);
    }
  }

  public static string ToStringWithSeparator<T>(this IEnumerable<T> list, string separator = ", ") => string.Join(separator, list);
}
