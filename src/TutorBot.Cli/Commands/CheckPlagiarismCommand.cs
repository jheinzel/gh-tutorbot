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

  private readonly Argument<string> rootDirectoryArgument = new("root-directory") { Description = "root directory containing submissions" };
  private readonly Option<string> languageOption = new("--language") { Description = "language", Aliases = { "-l" } };
  private readonly Option<string> reportFileOption = new("--report-file") { Description = "name of the report file", Aliases = { "-rf" } };
  private readonly Option<bool> refreshOption = new("--refresh") { Description = "redo check although results file exists", Aliases = { "-r" } };
  private readonly Option<string> baseCodeOption = new("--base-code") { Description = "Path to the template code directory", Aliases = { "-bc" } };

  private async Task HandleAsync(string rootDirectory, string languageOption, string? reportFileOption, bool refreshOption, string? baseCodeOption)
  {
    try
    {
      if (!Directory.Exists(rootDirectory))
      {
        throw new DomainException($"Error: Root directory \"{rootDirectory}\" does not exist. Clone assignment first.");
      }

      if (languageOption != "cpp" && languageOption != "java")
      {
        throw new DomainException($"Error: Unknown language \"{languageOption}\". Allowed values are 'cpp' and 'java'.");
      }

      string reportFile = reportFileOption ?? $"{rootDirectory}/{Constants.DEFAULT_REPORT_FILE}";

      if (baseCodeOption is not null
          && !Directory.Exists(baseCodeOption))
      {
        throw new DomainException($"Error: Given BaseCode Directory \"{baseCodeOption}\" does not exist.");
      }

      bool resultFileExists;
      if (refreshOption || !File.Exists(reportFile))
      {
        resultFileExists = await RunJplag(rootDirectory, reportFile, languageOption, baseCodeOption);
      }
      else
      {
        resultFileExists = true;
        Console.WriteLine($"Info: JPlag results file \"{reportFile}\" already exists. Skipping plagiarism check.");
        Console.WriteLine();
      }

      if (resultFileExists)
      {
        await PrintJPlagResults(reportFile);
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  private async Task<bool> RunJplag(string rootDirectory, string reportFile, string languageOption, string? baseCodeOption)
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
        throw new DomainException($"Error: JPlag jar file \"{configuration.JplagJarPath}\" does not exist. Download JPlag from https://github.com/jplag/JPlag/releases.\n" +
                                  $"       Ensure that the configuration parameter \"{ConfigurationHelper.KEY_JPLAG_JAR_PATH}\" is set appropriately.");
      }

      var jplagRunArgs = "";
      if (baseCodeOption is not null)
      {
        jplagRunArgs = string.Format(Constants.JPLAG_RUN_ARGS_BASE_DIR_PREFIX, baseCodeOption.TrimEnd(['/', '\\']));
      }
      
      jplagRunArgs += string.Format(Constants.JPLAG_RUN_ARGS, language, reportFile, rootDirectory);
      
      
      var javaArgs = $"-jar \"{configuration.JplagJarPath}\" {jplagRunArgs}";

      var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync(configuration.JavaPath, javaArgs);

      if (exitCode == 0)
      {
        File.WriteAllText($"{rootDirectory}/jplag.log", result);
        Console.WriteLine($"JPlag finished successfully. See \"{rootDirectory}/jplag.log\" for details.");
        Console.WriteLine($"Report written to \"{reportFile}\"");
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

      ZipArchiveEntry? entry = archive.GetEntry("topComparisons.json");
      if (entry is not null)
      {
        using (var jsonStream = entry.Open())
        {
          var comparisons = await JplagTopComparisonsDocument.FromJsonAsync(jsonStream);

          var printer = new TablePrinter();
          printer.AddRow("STUDENT-1", "STUDENT-2", "AVG-SIMILARITY", "MAX-SIMILARITY");

          foreach (var comparision in comparisons)
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

    Add(rootDirectoryArgument);

    languageOption.DefaultValueFactory = _ => "java";
    languageOption.AcceptOnlyFromAmong("cpp", "java");
    Options.Add(languageOption);

    Options.Add(reportFileOption);

    refreshOption.DefaultValueFactory = _ => false;
    Options.Add(refreshOption);
    
    Options.Add(baseCodeOption);

    Aliases.Add("cp");

    SetAction(async parsedResult =>
    {
      var rootDirectory = parsedResult.GetRequiredValue(rootDirectoryArgument);
      var language = parsedResult.GetRequiredValue(languageOption);
      var reportFile = parsedResult.GetValue(reportFileOption);
      var refresh = parsedResult.GetValue(refreshOption);
      var baseCode = parsedResult.GetValue(baseCodeOption);
      await HandleAsync(rootDirectory, language, reportFile, refresh, baseCode);
    });
  }
}

