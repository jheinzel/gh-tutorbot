using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class DownloadAssessmentsCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    void WriteHeader(StreamWriter writer, IEnumerable<string> exercises)
    {
      writer.Write($"\"Name\",\"Mat.Nr.\",\"Aufwand\"");
      foreach (var exercise in exercises)
      {
        foreach (var gradingType in new[] { "L", "I", "T" })
        {
          writer.Write($",\"{exercise} - {gradingType}\"");
        }
      }
      writer.WriteLine();
    }

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName, loadAssessments: true);

      var assessmentsFileName = string.Format(Constants.ASSESSMENTS_DOWNLOAD_FILE_NAME, assignment.Name);
      using var assessmentsFile = new StreamWriter(assessmentsFileName, append: false);

      int i = 0;
      foreach (var submission in assignment.Submissions.Where(s => s.Assessment.State == AssessmentState.Loaded))
      {
        try
        {
          var assessment = submission.Assessment ?? throw new InvalidOperationException($"Inconsitent Assessment state in sumbission {submission.RepositoryName}");

          if (i == 0)
          {
            WriteHeader(assessmentsFile, assessment.Lines.Select(line => line.Exercise));
          }

          assessmentsFile.Write($"\"{submission.Owner.FullName}\",{submission.Owner.MatNr},{assessment.Effort.ToString(CultureInfo.InvariantCulture)}");
          foreach (var line in assessment.Lines)
          {
            assessmentsFile.Write($",{line.Gradings[0]},{line.Gradings[1]},{line.Gradings[2]}");
          }

          assessmentsFile.WriteLine();
          i++;
        }
        catch (AssessmentFileException ex)
        {
          Console.Error.WriteRedLine($"{ex.Message}");
        }
      }

      Console.WriteLine($"Downloaded {i} {(i==1 ? "assessment" : "assessments")} to \"{assessmentsFileName}\"");
    }
    catch (Exception ex) when (ex is LogicException || ex is InfrastrucureException)
    {
      Console.Error.WriteRedLine($"{ex.Message}");
    }
    catch (ApiException apiEx)
    {
      Console.Error.WriteRedLine($"HTTP {(int)apiEx.StatusCode}: {apiEx.Message} ({apiEx.ApiError.DocumentationUrl})");
    }
  }

  public DownloadAssessmentsCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListSubmissionsCommand> logger) :
    base("download-assessments", "Download assessments of all submissions")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    AddAlias("da");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption);
  }
}

