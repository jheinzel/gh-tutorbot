using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.TextWriterExtensions;

namespace TutorBot.Utility;

public static class AuthHelper
{
  public async static Task<string> GetPersonalAccessTokenAsync()
  {
    try
    {
      var (result, _, exitCode) = await ProcessHelper.RunProcessAsync("gh", "auth token");
      if (exitCode != 0 || result is null)
      {
        Console.Error.WriteRedLine("No personal access token defined: Use \"gh auth login\" to login");
        Environment.Exit(2);
      }
      return result.Trim();
    }
    catch (InfrastrucureException)
    {
      Console.Error.WriteRedLine("Error: Command \"gh\" (GitHub CLI) not found");
      Environment.Exit(1);
    }

    Environment.Exit(3);
    return null!;
  }
}
