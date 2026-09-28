using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure;

public record ProcessResult(string? Result, string? ErrorResult, int ExitCode);
public record ProcessHandle(Task<ProcessResult> Task, Process Process);

public static class ProcessHelper
{
  // Standard usage: no process handle returned
  public static Task<ProcessResult> RunProcessAsync(string programPath, string argString)
  {
    return RunProcessInternalAsync(programPath, argString).Task;
  }

  // Advanced usage: process handle returned for termination
  public static ProcessHandle RunProcessWithHandleAsync(string programPath, string argString)
  {
    return RunProcessInternalAsync(programPath, argString);
  }

  // Shared implementation
  private static ProcessHandle RunProcessInternalAsync(string programPath, string argString)
  {
    void AppendLine(StringBuilder sb, string? line)
    {
      if (line is null) return;
      if (sb.Length > 0) sb.AppendLine();
      sb.Append(line);
    }

    var output = new StringBuilder();
    var error = new StringBuilder();

    try
    {
      var tcs = new TaskCompletionSource<ProcessResult>();

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

      process.OutputDataReceived += (sender, args) => AppendLine(output, args.Data);
      process.ErrorDataReceived += (sender, args) => AppendLine(error, args.Data);

      process.Start();
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();
      _ = Task.Run(async () =>
      {
        await process.WaitForExitAsync().ConfigureAwait(false);
        tcs.SetResult(new ProcessResult(output.ToString(), error.ToString(), process.ExitCode));
        process.Dispose();
      });

      return new ProcessHandle(tcs.Task, process);
    }
    catch (Win32Exception)
    {
      throw new CommandNotFoundException(programPath);
    }
  }
}
