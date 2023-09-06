using System.Diagnostics;

namespace TutorBot.Infrastructure;

public static class ProcessHelper
{
  public static Task<(string? result, int exitCode)> RunProcessAsync(string programPath, string argString)
  {
    var tcs = new TaskCompletionSource<(string? result, int exitCode)>();

    var process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = programPath,
        Arguments = argString,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
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
}
