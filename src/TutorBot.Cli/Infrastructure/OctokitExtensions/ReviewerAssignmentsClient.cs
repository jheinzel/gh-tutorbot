using System.Text.Json;
using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public sealed class ReviewerAssignmentsClient(IRepositoryContentsClient repositoryContents) : IReviewerAssignmentsClient
{
  private static readonly JsonSerializerOptions jsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
  };

  public async Task<ReviewerAssignmentsFile> Get(string org, string classroomName, string assignmentSlug)
  {
    var path = GetPath(classroomName, assignmentSlug);

    IReadOnlyList<RepositoryContent> contents;
    try
    {
      contents = await repositoryContents.GetAllContents(org, Constants.CLASSROOM_METADATA_REPO_NAME, path);
    }
    catch (NotFoundException)
    {
      return new ReviewerAssignmentsFile { AssignmentSlug = assignmentSlug };
    }

    var content = contents.Single();
    var reviewerAssignments = JsonSerializer.Deserialize<ReviewerAssignmentsFile>(content.Content, jsonOptions)
                              ?? throw new InvalidDataException($"Reviewer assignments file '{path}' is empty.");

    if (reviewerAssignments.SchemaVersion != 1)
    {
      throw new InvalidDataException($"Unsupported reviewer assignments schema version '{reviewerAssignments.SchemaVersion}' in '{path}'.");
    }

    if (!string.Equals(reviewerAssignments.AssignmentSlug, assignmentSlug, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidDataException($"Reviewer assignments file '{path}' declares a different assignment slug.");
    }

    return Normalize(reviewerAssignments);
  }

  public async Task Save(string org, string classroomName, ReviewerAssignmentsFile reviewerAssignments)
  {
    var path = GetPath(classroomName, reviewerAssignments.AssignmentSlug);
    var normalized = Normalize(reviewerAssignments);
    var content = JsonSerializer.Serialize(normalized, jsonOptions);
    var message = $"Update reviewer assignments for {reviewerAssignments.AssignmentSlug}";

    try
    {
      var existingFile = (await repositoryContents.GetAllContents(org, Constants.CLASSROOM_METADATA_REPO_NAME, path)).Single();
      await repositoryContents.UpdateFile(org,
                                          Constants.CLASSROOM_METADATA_REPO_NAME,
                                          path,
                                          new UpdateFileRequest(message, content, existingFile.Sha));
    }
    catch (NotFoundException)
    {
      await repositoryContents.CreateFile(org,
                                           Constants.CLASSROOM_METADATA_REPO_NAME,
                                           path,
                                           new CreateFileRequest(message, content));
    }
  }

  private static ReviewerAssignmentsFile Normalize(ReviewerAssignmentsFile reviewerAssignments)
  {
    var repositories = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    foreach (var (repository, reviewers) in reviewerAssignments.Repositories)
    {
      repositories[repository] = reviewers
        .Where(reviewer => !string.IsNullOrWhiteSpace(reviewer))
        .Select(reviewer => reviewer.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    return new ReviewerAssignmentsFile
    {
      SchemaVersion = 1,
      AssignmentSlug = reviewerAssignments.AssignmentSlug,
      Repositories = repositories
    };
  }

  private static string GetPath(string classroomName, string assignmentSlug)
  {
    ValidatePathSegment(classroomName, nameof(classroomName));
    ValidatePathSegment(assignmentSlug, nameof(assignmentSlug));
    return $"{classroomName}/reviewers/{assignmentSlug}.json";
  }

  private static void ValidatePathSegment(string value, string parameterName)
  {
    if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Contains('/') || value.Contains('\\'))
    {
      throw new ArgumentException("Value must be a non-empty path segment.", parameterName);
    }
  }
}
