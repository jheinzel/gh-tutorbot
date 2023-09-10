namespace TutorBot.Logic;

public class Submission
{
  public required long RepositoryId { get; init; }
  public required string RepositoryName { get; init; }
  public required string RepositoryFullName { get; init; }
  public required string RepositoryUrl { get; init; }
  public required Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; } = new List<Reviewer>();
}
