using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListSubmissionsCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    var printer = new TablePrinter();
    printer.AddRow("STUDENT", "MAT.NR.", "REVIEWER(S)", "ASSESSMENT", "REPOSITORY-URL");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName, loadAssessments: true);

      foreach (var submission in assignment.Submissions)
      {
        var reviewers = string.Join(", ", submission.Reviewers.Select(r => r.FullName));
        var assessmentInfo = submission.Assessment.State == AssessmentState.Loaded ? submission.Assessment.TotalGrading.ToString("F1", CultureInfo.InvariantCulture) : submission.Assessment.State.ToString();
        printer.AddRow(submission.Owner.FullName, submission.Owner.MatNr, reviewers, assessmentInfo, submission.RepositoryUrl);
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListSubmissionsCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListSubmissionsCommand> logger) :
    base("list-submissions", "List all submissions of an assignment")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    AddAlias("ls");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption);
  }
}

