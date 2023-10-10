using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Infrastructure;

public static class ProcessHelper
{
  public static Task<(string? Result, string? ErrorResult, int ExitCode)> RunProcessAsync(string programPath, string argString)
  {
    void AppendLine(StringBuilder sb, string? line)
    {
      if (sb.Length > 0) sb.AppendLine();
      sb.Append(line);
    }

    var output = new StringBuilder();
    var error = new StringBuilder();

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

      process.OutputDataReceived += (sender, args) => AppendLine(output, args.Data);
      process.ErrorDataReceived += (sender, args) => AppendLine(error, args.Data);

      process.Exited += (sender, args) =>
      {
        tcs.SetResult((output.ToString(), error.ToString(), process.ExitCode));
        process.Dispose();
      };

      process.Start();

      process.BeginOutputReadLine(); // Start async read of stdout
      process.BeginErrorReadLine(); // Start async read of stderr

      return tcs.Task;
    }
    catch (Win32Exception)
    {
      throw new CommandNotFoundException(programPath);
    }
  }
}
