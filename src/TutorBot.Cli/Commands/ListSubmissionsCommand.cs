using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListSubmissionsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    var printer = new TablePrinter();
    printer.AddRow("STUDENT", "STUD.ID", "REVIEWER(S)", "EFFORT", "ASSESSMENT", "REPOSITORY URL");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progress = new ProgressBar();
      var parameters = new AssigmentParameters(classroom.Id, assignmentName, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();
      
      foreach (var submission in assignment.Submissions.OrderBy(s => s.Owner.FullName))
      {
        var reviewers = submission.Reviewers.Select(r => r.FullName).ToStringWithSeparator();
        var effortInfo = submission.Assessment.State == AssessmentState.Loaded ? FormattableString.Invariant($"{submission.Assessment.Effort,6:F1}") : $"{"   -",-6}";
        var assessmentInfo = submission.Assessment.State == AssessmentState.Loaded ? FormattableString.Invariant($"{submission.Assessment.Total,10:F1}") : $"{submission.Assessment.State,-10}";
        printer.AddRow(submission.Owner.FullName, submission.Owner.MatNr, reviewers, effortInfo, assessmentInfo, submission.RepositoryUrl);
      }

      printer.Print();

      PrintStatistics(assignment);
      PrintUnlinked(assignment);
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  private static void PrintStatistics(Assignment assignment)
  {
    if (assignment.Submissions.Any())
    {
      var validSubmissions = assignment.Submissions.Where(s => s.Assessment.IsValid());
      
      Console.WriteLine();
      Console.WriteLine($"#submissions:        {assignment.Submissions.Count}");
      Console.WriteLine($"#valid submissions:  {validSubmissions.Count()}");
      if (validSubmissions.Any())
      {
        Console.WriteLine($"average effort:      {validSubmissions.Average(s => s.Assessment.Effort).ToString("F1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"average assessment:  {validSubmissions.Average(s => s.Assessment.Total).ToString("F1", CultureInfo.InvariantCulture)}");
      }
    }
    else
    {
      Console.WriteLine($"No submission for this assignment.");
    }
  }

  private static void PrintUnlinked(Assignment assignment)
  {
    if (assignment.UnlinkedSubmissions.Any())
    {
      Console.WriteLine();
      var unlinkedSubmissions = assignment.UnlinkedSubmissions.Select(s => s.RepositoryName).ToStringWithSeparator();
      Console.Out.WriteRedLine($"Unlinked submissions: {unlinkedSubmissions}. Check if the student roster file is up-to-date!");
    }
  }

  public ListSubmissionsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration, ILogger<ListSubmissionsCommand> logger) :
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

