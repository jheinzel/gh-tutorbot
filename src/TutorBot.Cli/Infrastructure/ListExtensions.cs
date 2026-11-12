namespace TutorBot.Infrastructure.CollectionExtensions;

public static class CollectionExtensions
{
  extension<T>(IList<T> list)
  {
    public void Shuffle()
    {
      var random = new Random();

      for (int i=list.Count-1; i > 0; i--)
      {
        int k = random.Next(i);
        (list[k], list[i]) = (list[i], list[k]);
      }
    }

  }

  extension<T>(IEnumerable<T> list)
  {
    public string ToStringWithSeparator(string separator = ", ") => string.Join(separator, list);
  }
}
