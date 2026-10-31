namespace TutorBot.Infrastructure.OctokitExtensions;

public sealed class ReviewerAssignmentsFile
{
  public int SchemaVersion { get; init; } = 1;
  public string AssignmentSlug { get; init; } = string.Empty;
  public Dictionary<string, List<string>> Repositories { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
