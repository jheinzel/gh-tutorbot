using System.Net;
using Octokit;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class ClassroomsClient : ApiClient, IClassroomsClient
{
  private class RepositoryContentEntryDto
  {
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? HtmlUrl { get; init; }
  }

  private IAssignmentsClient assignmentsClient;
  private ISubmissionsClient submissionsClient;

  public ClassroomsClient(IApiConnection apiConnection) : base(apiConnection)
  {
    assignmentsClient = new AssignmentsClient(new ApiConnection(Connection));
    submissionsClient = new SubmissionsClient(new ApiConnection(Connection));
  }

  public IAssignmentsClient Assignment => assignmentsClient;

  public ISubmissionsClient Submissions => submissionsClient;

  public async Task<IReadOnlyList<ClassroomDto>> GetAll(string org)
  {
    if (string.IsNullOrWhiteSpace(org))
    {
      throw new ArgumentException("Organization must be provided.", nameof(org));
    }

    var endpoint = new Uri($"repos/{org}/{Constants.CLASSROOM_METADATA_REPO_NAME}/contents/", UriKind.Relative);
    var parameters = new Dictionary<string, string>();

    IReadOnlyList<RepositoryContentEntryDto> entries;
    try
    {
      var response = await Connection.Get<IReadOnlyList<RepositoryContentEntryDto>>(endpoint, parameters);
      entries = response.Body;
    }
    catch (NotFoundException ex)
    {
      throw new ApiException(
        $"Could not load classrooms from metadata repository 'https://github.com/{org}/{Constants.CLASSROOM_METADATA_REPO_NAME}'. " +
        "Verify the organization name, that the repository exists, and that your token has access.",
        ex.StatusCode);
    }

    var classrooms = entries
      .Where(entry => entry.Type == "dir" && !string.Equals(entry.Name, ".github", StringComparison.OrdinalIgnoreCase))
      .Select((entry, index) => new ClassroomDto
      {
        Id = index + 1,
        Name = entry.Name,
        Url = entry.HtmlUrl,
        Archived = false
      })
      .ToList();

    return classrooms;
  }

  public async Task<ClassroomDto> GetById(long classroomId)
  {
    var endpoint = new Uri($"classrooms/{classroomId}", UriKind.Relative);
    var parameters = new Dictionary<string, string>();

    var response = await Connection.Get<ClassroomDto>(endpoint, parameters);

    if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
    {
      throw new ApiException($"Error retrieving classrooms", response.HttpResponse.StatusCode);
    }

    return response.Body;
  }

  public async Task<ClassroomDto> GetByName(string classroomName, string? org = null)
  {
    if (string.IsNullOrWhiteSpace(org))
    {
      throw new ArgumentException("Organization must be provided.", nameof(org));
    }

    var classroom = (await GetAll(org)).SingleOrDefault(c => c.Name == classroomName);
    if (classroom is null)
    {
      throw new ClassroomNotFoundException(classroomName);
    }

    return classroom;
  }
}
