using System.Net;
using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class SubmissionsClient : ApiClient, ISubmissionsClient
{
  private IRepositoriesClient repositoriesClient;

  public SubmissionsClient(IApiConnection apiConnection) : base(apiConnection)
  {
    this.repositoriesClient = new RepositoriesClient(new ApiConnection(Connection));
  }

  public IRepositoriesClient Repository => repositoriesClient;


  public async Task<IReadOnlyList<SubmissionDto>> GetAll(long assignmentId, IProgress? progress)
  {
    var pagingOptions = new PagingOptions { Page = 1, PerPage = Constants.SUBMISSIONS_PAGE_SIZE };
    
    var submissions = new List<SubmissionDto>();
    bool hasNextPage = true;

    while (hasNextPage)
    {
      var endpoint = new Uri($"assignments/{assignmentId}/accepted_assignments", UriKind.Relative);
      var response = await Connection.Get<List<SubmissionDto>>(endpoint, pagingOptions.ToDictionary());

      if (response.HttpResponse.StatusCode != HttpStatusCode.OK)
      {
        throw new ApiException($"Error retrieving submissions", response.HttpResponse.StatusCode);
      }

      submissions.AddRange(response.Body);

      progress?.Increment(response.Body.Count);

      pagingOptions.IncrementPage();
      hasNextPage = response.Body.Count >= pagingOptions.PerPage;
    }

    return submissions;
  }
}
