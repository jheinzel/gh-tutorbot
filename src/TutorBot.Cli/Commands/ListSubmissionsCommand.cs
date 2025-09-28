using System.CommandLine;
using System.Globalization;
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

  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment name" };
  private readonly Option<string> classroomOption = new("--classroom") { Description = "classroom name", Aliases = { "-c" } };
  private readonly Option<int?> groupOption = new("--group") { Description = "filter group", Aliases = { "-g" } };

  private async Task HandleAsync(string assignmentName, string classroomName, int? group)
  {
    var printer = new TablePrinter();
    printer.AddRow("STUDENT", "STUD.ID", "Gr.", "REVIEWER(S)", "EFFORT", "ASSESSMENT", "REPOSITORY URL");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName, group, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();

      foreach (var submission in assignment.Submissions.OrderBy(s => s.Owner.FullName))
      {
        var reviewers = submission.Reviewers.Select(r => r.FullName).ToStringWithSeparator();
        var effortInfo = submission.Assessment.State == AssessmentState.Loaded ? FormattableString.Invariant($"{submission.Assessment.Effort,6:F1}") : $"{"   -",-6}";
        var assessmentInfo = submission.Assessment.State == AssessmentState.Loaded ? FormattableString.Invariant($"{submission.Assessment.Total,10:F1}") : $"{submission.Assessment.State,-10}";
        printer.AddRow(submission.Owner.FullName,
                       submission.Owner.MatNr,
                       submission.Owner.GroupNr.ToString().PadLeft(3),
                       reviewers,
                       effortInfo,
                       assessmentInfo,
                       submission.RepositoryUrl);
      }

      printer.Print();

      PrintStatistics(assignment.Submissions);
      PrintUnlinked(assignment);
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  private static void PrintStatistics(IReadOnlyCollection<Submission> submissions)
  {
    if (submissions.Any())
    {
      var validSubmissions = submissions.Where(s => s.Assessment.IsValid());

      Console.WriteLine();
      Console.WriteLine($"#submissions:        {submissions.Count}");
      Console.WriteLine($"#valid submissions:  {validSubmissions.Count()}");
      if (validSubmissions.Any())
      {
        Console.WriteLine($"average effort:      {validSubmissions.Average(s => s.Assessment.Effort).ToString("F1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"average assessment:  {validSubmissions.Average(s => s.Assessment.Total).ToString("F1", CultureInfo.InvariantCulture)}");
      }
    }
    else
    {
      Console.WriteLine($"No submission for this assignment (in the specified group).");
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

  public ListSubmissionsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("list-submissions", "List all submissions of an assignment")
  {
    this.client = client;
    this.configuration = configuration;

    Add(assignmentArgument);

    classroomOption.DefaultValueFactory = _ => configuration.DefaultClassroom;
    Options.Add(classroomOption);

    groupOption.DefaultValueFactory = _ => null;
    Options.Add(groupOption);

    Aliases.Add("ls");

    SetAction(async parsedResult =>
    {
      var assignmentName = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetRequiredValue(classroomOption);
      var group = parsedResult.GetValue(groupOption);
      await HandleAsync(assignmentName, classroomName, group);
    });
  }
}

