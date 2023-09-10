using System.ComponentModel;
using System.Diagnostics;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure;

public static class ProcessHelper
{
  public static Task<(string? Result, string? ErrorResult, int ExitCode)> RunProcessAsync(string programPath, string argString)
  {
    try
    {
      var tcs = new TaskCompletionSource<(string?, string?, int)>();

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
        tcs.SetResult((await process.StandardOutput.ReadToEndAsync(), await process.StandardError.ReadToEndAsync(), process.ExitCode));
        process.Dispose();
      };

      process.Start();

      return tcs.Task;

    }
    catch (Win32Exception)
    {
      throw new CommandNotFoundException(programPath);
    }
  }
}
