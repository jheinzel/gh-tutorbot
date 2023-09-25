using System;
using System.CommandLine;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Domain.JPlag;
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
  private readonly Option<bool> refreshOption = new("--refresh", "redo check although results file exists");

  private async Task HandleAsync(string rootDirectory, string languageOption, string? reportFileOption, bool refreshOption)
  {
    try
    {
      if (!Directory.Exists(rootDirectory))
      {
        throw new LogicException($"Error: Root directory \"{rootDirectory}\" does not exist. Clone assignment first.");
      }

      string reportFile = reportFileOption ?? $"{rootDirectory}/{Constants.DEFAULT_REPORT_FILE}";
      string jplugResultFile = $"{reportFile}.zip";

      bool resultFileExists;
      if (refreshOption || !File.Exists(jplugResultFile))
      {
        resultFileExists = await RunJplag(rootDirectory, reportFile, languageOption);
      }
      else
      {
        resultFileExists = true;
        Console.WriteLine($"Info: JPlag results file \"{jplugResultFile}\" already exists. Skipping plagiarism check.");
      }

      if (resultFileExists)
      {
        await PrintJPlagResults(jplugResultFile);
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  private async Task<bool> RunJplag(string rootDirectory, string reportFile, string languageOption)
  {
    try
    {
      string language = languageOption switch
      {
        "cpp" => "cpp2",
        "java" => "java",
        _ => throw new LogicException($"Error: Unknown language \"{languageOption}\"")
      };

      if (!File.Exists(configuration.JplagJarPath))
      {
        throw new LogicException($"Error: JPlag jar file \"{rootDirectory}\" does not exist. Download JPlag from https://github.com/jplag/JPlag/releases.\n" +
                                 $"       Ensure that the configuration parameter \"{ConfigurationHelper.KEY_JPLAG_JAR_PATH}\" is set appropriately.");
      }

      var jplagArgs = string.Format(Constants.JPLAG_ARGS, language, reportFile, rootDirectory);
      var javaArgs = $"-jar \"{configuration.JplagJarPath}\" {jplagArgs}";
      var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync(configuration.JavaPath, javaArgs);

      if (exitCode == 0)
      {
        Console.WriteLine(result);
      }
      else
      {
        Console.WriteLine(result);
        Console.Error.WriteRedLine($"{errorResult}");
      }

      return exitCode == 0;
    }
    catch (CommandNotFoundException)
    {
      throw new LogicException($"Error: Java (\"{configuration.JavaPath}\") not found.");
    }
  }

  private static async Task PrintJPlagResults(string resultsZipFile)
  {
    try
    {
      var archive = ZipFile.Open(resultsZipFile, ZipArchiveMode.Read);

      ZipArchiveEntry ? entry = archive.GetEntry("overview.json");
      if (entry is not null)
      {
        using (var jsonStream = entry.Open())
        {
          var overviewDocument = await JPlagOverviewDocument.FromJsonAsync(jsonStream);

          var printer = new TablePrinter();
          printer.AddRow("STUDENT-1", "STUDENT-2", "SIMILARITY");

          foreach (var comparision in overviewDocument.Metrics[0].TopComparisons)
          {
            printer.AddRow($"{comparision.FirstSubmission}",
                            $"{comparision.SecondSubmission}",
                            FormattableString.Invariant($"{comparision.Similarity,9:F2}"));
          }

          printer.Print();
        }
      }
    }
    catch (FileNotFoundException ex)
    {
      throw new LogicException($"Error: JPlag results file \"{ex.FileName}\" not found.");
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

    reportFileOption.AddAlias("-rf");
    AddOption(reportFileOption);

    refreshOption.AddAlias("-r");
    refreshOption.SetDefaultValue(false);
    AddOption(refreshOption);


    AddAlias("cp");

    this.SetHandler(HandleAsync, rootDirectoryArgument, languageOption, reportFileOption, refreshOption);
  }
}

