using System.Net;
using Octokit;
using Octokit.Internal;

namespace TutorBot.Infrastructure.OctokitExtensions;

public static class OctokitExtensions
{
  public static IGitHubClient GetGitHubClient(string appName, string accessToken)
  {
    // Adapt the HttpClient, so that it sends an Accept-Encoding header without the gzip value.
    // gzip causes an HTTP 500 error, starting with September 20, 2023.
    HttpClientAdapter adapter = new HttpClientAdapter(() => new HttpClientHandler
    {
      AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.Brotli
    });

    var connection = new Connection(new ProductHeaderValue(appName),
      new HttpClientAdapter(() => new HttpClientHandler
      {
        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.Brotli
      }));

    var client = new GitHubClient(connection);
    client.Credentials = new Credentials(accessToken);

    return client;
  }

  // Unforunately, there are no extension properties in C# up to now.
  public static IClassroomsClient Classroom(this IGitHubClient client) {
    return new ClassroomsClient(new ApiConnection(client.Connection));
  }
}
