using System.Net;
using Octokit;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class ClassroomsClient : ApiClient, IClassroomsClient
{
  private IAssignmentsClient assignmentsClient;
  private ISubmissionsClient submissionsClient;

  public ClassroomsClient(IApiConnection apiConnection) : base(apiConnection)
  {
    assignmentsClient = new AssignmentsClient(new ApiConnection(Connection));
    submissionsClient = new SubmissionsClient(new ApiConnection(Connection));
  }

  public IAssignmentsClient Assignment => assignmentsClient;

  public ISubmissionsClient Submissions => submissionsClient;

  public async Task<IReadOnlyList<ClassroomDto>> GetAll()
  {
    var endpoint = new Uri("classrooms", UriKind.Relative);
    var parameters = new Dictionary<string, string>();

    var response = await Connection.Get<IReadOnlyList<ClassroomDto>>(endpoint, parameters);

    if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
    {
      throw new ApiException($"Error retrieving classrooms", response.HttpResponse.StatusCode);
    }

    return response.Body;
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

  public async Task<ClassroomDto> GetByName(string classroomName)
  {
    var classroom = (await GetAll()).SingleOrDefault(c => c.Name == classroomName);
    if (classroom is null)
    {
      throw new ClassroomNotFoundException(classroomName);
    }

    return classroom;
  }
}
