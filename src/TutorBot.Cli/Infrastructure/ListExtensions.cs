namespace TutorBot.Infrastructure.ListExtensions;

public static class ListExtensions
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
}
