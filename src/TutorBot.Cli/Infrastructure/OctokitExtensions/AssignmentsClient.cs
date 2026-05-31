using System.Net;
using System.Text;
using System.Text.Json;
using Octokit;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class AssignmentsClient : ApiClient, IAssignmentsClient
{
  private class RepositoryFileContentDto
  {
    public string Content { get; init; } = string.Empty;
    public string Encoding { get; init; } = string.Empty;
  }

  private class AssignmentFileDto
  {
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Due { get; init; } = string.Empty;
  }

  private class AssignmentsRootDto
  {
    public IReadOnlyList<AssignmentFileDto> Assignments { get; init; } = [];
  }

  public AssignmentsClient(IApiConnection apiConnection) : base(apiConnection)
  {
  }

  public async Task<IReadOnlyList<AssignmentDto>> GetAll(long classroomId)
  {
    var endpoint = new Uri($"classrooms/{classroomId}/assignments", UriKind.Relative);
    var parameters = new Dictionary<string, string>();

    var response = await Connection.Get<IReadOnlyList<AssignmentDto>>(endpoint, parameters);

    if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
    {
      throw new ApiException($"Error retrieving assignments", response.HttpResponse.StatusCode);
    }

    return response.Body;
  }

  public async Task<AssignmentDto> GetBySlug(long classroomId, string assignmentSlug)
  {
    var assignment = (await GetAll(classroomId)).SingleOrDefault(a => a.Slug == assignmentSlug);
    if (assignment is null)
    {
      throw new AssignmentNotFoundException(assignmentSlug);
    }

    return assignment;
  }

  public async Task<IReadOnlyList<AssignmentDto>> GetAll(string org, string classroomName)
  {
    if (string.IsNullOrWhiteSpace(org))
    {
      throw new ArgumentException("Organization must be provided.", nameof(org));
    }

    if (string.IsNullOrWhiteSpace(classroomName))
    {
      throw new ArgumentException("Classroom name must be provided.", nameof(classroomName));
    }

    var endpoint = new Uri($"repos/{org}/{Constants.CLASSROOM_METADATA_REPO_NAME}/contents/{classroomName}/assignments.json", UriKind.Relative);
    var response = await Connection.Get<RepositoryFileContentDto>(endpoint, new Dictionary<string, string>());

    if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
    {
      throw new ApiException("Error retrieving assignments", response.HttpResponse.StatusCode);
    }

    if (!string.Equals(response.Body.Encoding, "base64", StringComparison.OrdinalIgnoreCase))
    {
      throw new InfrastrucureException($"Unsupported assignments.json encoding \"{response.Body.Encoding}\".");
    }

    var base64Content = response.Body.Content.Replace("\n", string.Empty).Replace("\r", string.Empty);
    var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Content));

    var root = JsonSerializer.Deserialize<AssignmentsRootDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new AssignmentsRootDto();

    var classroomPrefix = $"{classroomName}-";
    var repoCandidates = new List<Repository>();
    var page = 1;
    const int perPage = 100;

    while (true)
    {
      var parameters = new Dictionary<string, string>
      {
        ["q"] = $"{classroomPrefix} in:name org:{org}",
        ["per_page"] = perPage.ToString(),
        ["page"] = page.ToString()
      };

      var searchResult = await Connection.Get<SearchRepositoryResult>(new Uri("search/repositories", UriKind.Relative), parameters);
      repoCandidates.AddRange(searchResult.Body.Items);

      if (searchResult.Body.Items.Count < perPage)
      {
        break;
      }

      page++;
    }

    var repos = repoCandidates
      .GroupBy(r => r.Id)
      .Select(g => g.First())
      .Where(r => r.Name.StartsWith(classroomPrefix, StringComparison.OrdinalIgnoreCase))
      .ToList();

    return root.Assignments
      .Select((assignment, index) =>
      {
        DateTimeOffset? deadline = null;
        if (!string.IsNullOrWhiteSpace(assignment.Due) && DateTimeOffset.TryParse(assignment.Due, out var parsedDue))
        {
          deadline = parsedDue;
        }

        var slugPart = $"-{assignment.Slug}-";
        var accepted = repos.Count(r => r.Name.Contains(slugPart, StringComparison.OrdinalIgnoreCase));

        return new AssignmentDto
        {
          Id = index + 1,
          Title = assignment.Name,
          Slug = assignment.Slug,
          Accepted = accepted,
          Deadline = deadline
        };
      })
      .ToList();
  }

  public async Task<AssignmentDto> GetBySlug(string org, string classroomName, string assignmentSlug)
  {
    var assignment = (await GetAll(org, classroomName)).SingleOrDefault(a => a.Slug == assignmentSlug);
    if (assignment is null)
    {
      throw new AssignmentNotFoundException(assignmentSlug);
    }

    return assignment;
  }
}
