using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class CheckPlagiarismCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> rootDirectoryArgument = new("root-directory", "root directory containing submissions");
  private readonly Option<string> languageOption = new("--language", "language");
  private readonly Option<string> reportFileOption = new("--report-file", "name of the file in which the comparison results will be stored");

  private async Task HandleAsync(string rootDirectory, string languageOption, string? reportFileOption)
  {
    try
    {
      if (!Directory.Exists(rootDirectory))
      {
        throw new LogicException($"Error: Root directory \"{rootDirectory}\" does not exist. Clone assignment first.");
      }

      if (!File.Exists(configuration.JplagJarPath))
      {
         throw new LogicException($"Error: JPlag jar file \"{rootDirectory}\" does not exist. Download JPlag from https://github.com/jplag/JPlag/releases.\n" + 
                                  $"       Ensure that the configuration parameter \"{ConfigurationHelper.KEY_JPLAG_JAR_PATH}\" is set appropriately.");
      }

      string language = languageOption switch
      {
        "cpp" => "cpp2",
        "java" => "java",
        _ => throw new LogicException($"Error: Unknown language \"{languageOption}\"")
      };

      string reportFile = reportFileOption ?? $"{rootDirectory}/{Constants.DEFAULT_REPORT_FILE}";

      var jplagArgs = string.Format(Constants.JPLAG_ARGS, language, reportFile, rootDirectory);
      var javaArgs = $"-jar \"{configuration.JplagJarPath}\" {jplagArgs}";
      var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync(configuration.JavaPath, javaArgs);

      if (exitCode == 0)
      {
        Console.WriteLine(result);
      }
      else
      {
        Console.Error.WriteRedLine($"{errorResult}");
      }
    }
    catch (CommandNotFoundException)
    {
      Console.Error.WriteRedLine($"Error: Java (\"{configuration.JavaPath}\") not found");
    }
    catch (Exception ex) when (ex is LogicException || ex is InfrastrucureException)
    {
      Console.Error.WriteRedLine($"{ex.Message}");
    }
  }


  public CheckPlagiarismCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) :
    base("check-plagiarism", "Check for plagiarism")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(rootDirectoryArgument);

    languageOption.AddAlias("-l");
    languageOption.FromAmong("cpp", "java")
              .SetDefaultValue("cpp");
    AddOption(languageOption);

    reportFileOption.AddAlias("-r");
    AddOption(reportFileOption);

    AddAlias("cp");

    this.SetHandler(HandleAsync, rootDirectoryArgument, languageOption, reportFileOption);
  }
}

