namespace TutorBot.Infrastructure.OctokitExtensions;

public class RepositoryDto
{
  public long Id { get; init; }
}

public class StudentDto
{
  public long Id { get; init; }
  public string Login { get; init; } = string.Empty;
}

public class SubmissionDto
{
  public long Id { get; init; }
	public int CommitCount { get; init; }
  public RepositoryDto? Repository { get; init; }
  public IReadOnlyList<StudentDto> Students { get; init; } = Array.Empty<StudentDto>();
}
