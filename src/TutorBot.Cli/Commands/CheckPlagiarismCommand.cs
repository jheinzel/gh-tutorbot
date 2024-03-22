using System.CommandLine;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using TutorBot.Domain.Exceptions;
using TutorBot.Domain.JPlag;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class CheckPlagiarismCommand : Command
{
  private readonly IGitHubClassroomClient client;
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
        throw new DomainException($"Error: Root directory \"{rootDirectory}\" does not exist. Clone assignment first.");
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
        Console.WriteLine();
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
        "cpp" => "cpp",
        "java" => "java",
        _ => throw new DomainException($"Error: Unknown language \"{languageOption}\"")
      };

      if (!File.Exists(configuration.JplagJarPath))
      {
        throw new DomainException($"Error: JPlag jar file \"{rootDirectory}\" does not exist. Download JPlag from https://github.com/jplag/JPlag/releases.\n" +
                                 $"       Ensure that the configuration parameter \"{ConfigurationHelper.KEY_JPLAG_JAR_PATH}\" is set appropriately.");
      }

      var jplagArgs = string.Format(Constants.JPLAG_ARGS, language, reportFile, rootDirectory);
      var javaArgs = $"-jar \"{configuration.JplagJarPath}\" {jplagArgs}";

      var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync(configuration.JavaPath, javaArgs);

      if (exitCode == 0)
      {
        File.WriteAllText($"{rootDirectory}/jplag.log", result);
        Console.WriteLine($"JPlag finished successfully. See \"{rootDirectory}/jplag.log\" for details.");
        Console.WriteLine("Display the results with the report viewer at https://jplag.github.io/JPlag/");
        Console.WriteLine();
      }
      else
      {
        File.WriteAllText($"{rootDirectory}/jplag.log", result);
        File.WriteAllText($"{rootDirectory}/jplag.log", errorResult);
        Console.WriteLine($"JPlag finished with errors. See \"{rootDirectory}/jplag.log\" and \"{rootDirectory}/jplag-errors.log\" for details.");
        Console.WriteLine();
      }

      return exitCode == 0;
    }
    catch (CommandNotFoundException)
    {
      throw new DomainException($"Error: Java (\"{configuration.JavaPath}\") not found.");
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
          var overviewDocument = await JplagOverviewDocument.FromJsonAsync(jsonStream);

          var printer = new TablePrinter();
          printer.AddRow("STUDENT-1", "STUDENT-2", "AVG-SIMILARITY", "MAX-SIMILARITY");

          foreach (var comparision in overviewDocument.TopComparisons)
          {
            if (comparision.Similarities is null)
            {
							throw new DomainException($"Error: Unexpected structure of 'overview.json'. No 'similarity' property in 'top-comparisons' element.");
						}

            printer.AddRow($"{comparision.FirstSubmission}",
                           $"{comparision.SecondSubmission}",
                           FormattableString.Invariant($"{comparision.Similarities.Avg*100,10:F1}"),
													 FormattableString.Invariant($"{comparision.Similarities.Max*100,10:F1}"));
          }

          printer.Print();
        }
      }
    }
    catch (FileNotFoundException ex)
    {
      throw new DomainException($"Error: JPlag results file \"{ex.FileName}\" not found.");
    }
  }

  public CheckPlagiarismCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
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

