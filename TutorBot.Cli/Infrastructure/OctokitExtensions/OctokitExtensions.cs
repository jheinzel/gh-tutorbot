using System.Net;
using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public static class OctokitExtensions
{
  // Unforunately, there are no extension properties in C# up to now.
  public static IClassroomsClient Classroom(this IGitHubClient client) {
    return new ClassroomsClient(new ApiConnection(client.Connection));
  }
}
