namespace TutorBot.Infrastructure.OctokitExtensions;

public class PagingOptions
{
  public required int Page { get; set; }
  public required int PerPage { get; set; }

  public int IncrementPage() => ++Page;

  public IDictionary<string, string> ToDictionary()
  {
    return new Dictionary<string, string>
    {
      { Constants.PAGE_KEY, Page.ToString() },
      { Constants.PER_PAGE_KEY, PerPage.ToString() }
    };
  }
}