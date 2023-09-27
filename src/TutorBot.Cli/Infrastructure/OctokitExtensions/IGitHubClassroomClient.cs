using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IGitHubClassroomClient : IGitHubClient
{
  IClassroomsClient Classroom { get; }
}
