namespace TutorBot.Logic;

public class Submission
{
  public required string RepositoryName { get; init; }
  public required string RepositoryUrl { get; init; }
  public required Student Student { get; init; }

  public IList<Student> Reviewers { get; set; } = new List<Student>();
}
