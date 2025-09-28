using System.CommandLine;
using System.Diagnostics;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ViewPlagiarismReportCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Option<string> reportFileOption = new("--report-file") { Description = "report file path", Aliases = { "-rf" } };

  private async Task HandleAsync(string reportFile)
  {
    try
    {
      if (File.Exists(reportFile))
      {
        await RunJplagViewer(reportFile);
      }
      else
      {
        Console.WriteLine($"Info: JPlag results file \"{reportFile}\" does not exist. Run plagiarism check first.");
        Console.WriteLine();
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  private async Task RunJplagViewer(string reportFile)
  {
    try
    {

      if (!File.Exists(configuration.JplagJarPath))
      {
        throw new DomainException($"Error: JPlag jar file \"{configuration.JplagJarPath}\" does not exist. Download JPlag from https://github.com/jplag/JPlag/releases.\n" +
                                  $"       Ensure that the configuration parameter \"{ConfigurationHelper.KEY_JPLAG_JAR_PATH}\" is set appropriately.");
      }

      var jplagViewArgs = string.Format(Constants.JPLAG_VIEW_ARGS, reportFile);
      var javaArgs = $"-jar \"{configuration.JplagJarPath}\" {jplagViewArgs}";

      var (task, process) = ProcessHelper.RunProcessWithHandleAsync(configuration.JavaPath, javaArgs);
      Console.WriteLine("Press Ctrl-C to terminate the JPlag server ...");

      Console.CancelKeyPress += (sender, e) =>
      {
        process.Kill();
        Console.WriteLine("Shutting down JPlag server ...");
        e.Cancel = true; 
      };

      await task;
    }
    catch (CommandNotFoundException)
    {
      throw new DomainException($"Error: Java (\"{configuration.JavaPath}\") not found.");
    }
  }

  public ViewPlagiarismReportCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("view-plagiarism-report", "View plagiarism report")
  {
    this.client = client;
    this.configuration = configuration;

    reportFileOption.DefaultValueFactory = _ => $"./{Constants.DEFAULT_REPORT_FILE}";
    Options.Add(reportFileOption);

    Aliases.Add("vr");

    SetAction(async parsedResult =>
    {
      var reportFile = parsedResult.GetValue(reportFileOption);
      await HandleAsync(reportFile);
    });
  }
}

