using System.ComponentModel;
using System.Diagnostics;

namespace TutorBot.Utility;

internal class AuthHelper
{
  private static Task<(string? result, int exitCode)> RunProcessAsync(string programPath, string argString)
  {
    var tcs = new TaskCompletionSource<(string? result, int exitCode)>();

    var process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = programPath,
        Arguments = argString,
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
      },
      EnableRaisingEvents = true
    };

    process.Exited += async (sender, args) =>
    {
      tcs.SetResult((await process.StandardOutput.ReadLineAsync(), process.ExitCode));
      process.Dispose();
    };

    process.Start();

    return tcs.Task;
  }

  public async static Task<string> GetPersonalAccessTokenAsync()
  {
    try
    {
      var (result, exitCode) = await RunProcessAsync("gh", "auth token");
      if (exitCode != 0)
      {
        Console.WriteLine("No personal access token defined. Use \"gh auth login\"");
        Environment.Exit(2);
      }
      return result!;
    }
    catch (Win32Exception)
    {
      Console.Error.WriteLine("Error: Command \"gh\" (GitHub CLI) not found");
      Environment.Exit(1);
    }

    Environment.Exit(3);
    return null!;
  }
}
