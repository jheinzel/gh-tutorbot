using System.Net;
using Octokit;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class AssignmentsClient : ApiClient, IAssignmentsClient
{
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

  public async Task<AssignmentDto> GetByName(long classroomId, string assignmentName)
  {
    var assignment = (await GetAll(classroomId)).SingleOrDefault(a => a.Title == assignmentName);
    if (assignment is null)
    {
      throw new AssignmentNotFoundException(assignmentName);
    }

    return assignment;
  }
}
