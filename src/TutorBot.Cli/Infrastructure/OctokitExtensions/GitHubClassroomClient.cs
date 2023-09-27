using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class GitHubClassroomClient : GitHubClient, IGitHubClassroomClient
{
  public GitHubClassroomClient(string productInformation, string accessToken) : base(new ProductHeaderValue(productInformation))
  {
    // Adapt the HttpClient, so that it sends an Accept-Encoding header without the gzip value.
    // gzip causes an HTTP 500 error, starting with September 20, 2023.
    //HttpClientAdapter adapter = new HttpClientAdapter(() => new HttpClientHandler
    //{
    //  AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.Brotli
    //});

    // this.connection = new Connection(new ProductHeaderValue(appName),
    //  new HttpClientAdapter(() => new HttpClientHandler
    //  {
    //    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.Brotli
    //  }));

    // Turn on gzip encoding, since the Github API gzip bug was fixed on September 22, 2023. 

    Credentials = new Credentials(accessToken);
  }

  public IClassroomsClient Classroom => new ClassroomsClient(new ApiConnection(base.Connection));
}
