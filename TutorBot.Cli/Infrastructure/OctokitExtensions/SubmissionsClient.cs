using System.Net;
using Octokit;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class SubmissionsClient : ApiClient, ISubmissionsClient
{
  private IRepositoriesClient repositoriesClient;

  public SubmissionsClient(IApiConnection apiConnection) : base(apiConnection)
  {
    this.repositoriesClient = new RepositoriesClient(new ApiConnection(Connection));
  }

  public IRepositoriesClient Repository => repositoriesClient;


  public async Task<IReadOnlyList<Repository>> GetAll(long assignmentId)
  {
    var endpoint = new Uri($"assignments/{assignmentId}/accepted_assignments", UriKind.Relative);
    var parameters = new Dictionary<string, string>();

    var response = await Connection.Get<IReadOnlyList<SubmissionDto>>(endpoint, parameters);

    if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
    {
      throw new ApiException($"Error retrieving assignments", response.HttpResponse.StatusCode);
    }

    var repositoryIds = response.Body.Where(s => s.Repository is not null)
                                     .Select(s => s.Repository!.Id).ToList();

    
    return await Task.WhenAll(repositoryIds.Select(id => this.Repository.Get(id)));
  }
}
