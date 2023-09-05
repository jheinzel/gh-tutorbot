namespace TutorBot.Infrastructure.OctokitExtensions;

public class RepositoryDto
{
  public long Id { get; init; }
}

public class SubmissionDto
{
  public long Id { get; init; }
	public int CommitCount { get; init; }
  public RepositoryDto? Repository { get; init; }
}
