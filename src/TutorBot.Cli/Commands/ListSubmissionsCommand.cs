using System.CommandLine;
using System.Globalization;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListSubmissionsCommand : ClassroomCommand
{
  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment slug" };
  private readonly Option<int?> groupOption = new("--group") { Description = "filter group", Aliases = { "-g" } };

  private async Task HandleAsync(string assignmentSlug, string? classroomName, string? org, int? group)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    var printer = new TablePrinter();
    printer.AddRow("STUDENT", "STUD.ID", "Gr.", "REVIEWER(S)", "EFFORT", "ASSESSMENT", "REPOSITORY URL");

    try
    {
      var studentList = await StudentList.FromGitHub(Client, org, classroomName);
      var classroom = await Client.Classroom.GetByName(classroomName, org);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentSlug, group, ClassroomName: classroomName, Org: org, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(Client, studentList, parameters, Configuration, progress);
      progress.Dispose();

      foreach (var submission in assignment.Submissions.OrderBy(s => s.Owner.FullName))
      {
        var reviewers = submission.Reviewers.Select(r => r.FullName).ToStringWithSeparator();
        var effortStr = submission.Assessment.State == AssessmentState.Loaded ? 
          FormattableString.Invariant($"{submission.Assessment.Effort,6:F1}") : 
          "   -  ";
        var assessmentStr = submission.Assessment.State == AssessmentState.Loaded ? 
          FormattableString.Invariant($"{submission.Assessment.Total,10:F1}") : 
          submission.Assessment.State.ToString().PadRight(10);

        printer.AddRow(submission.Owner.FullName,
                       submission.Owner.MatNr,
                       submission.Owner.GroupNr.ToString().PadLeft(3),
                       reviewers,
                       effortStr,
                       assessmentStr,
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
    base("list-submissions", "List all submissions of an assignment", client, configuration)
  {
    SetupCommonOptionDefaults();

    Add(assignmentArgument);
    Options.Add(ClassroomOption);
    Options.Add(OrgOption);

    groupOption.DefaultValueFactory = _ => null;
    Options.Add(groupOption);

    Aliases.Add("ls");

    SetAction(async parsedResult =>
    {
      var assignmentSlug = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetValue(ClassroomOption);
      var org = parsedResult.GetValue(OrgOption);
      var group = parsedResult.GetValue(groupOption);
      await HandleAsync(assignmentSlug, classroomName, org, group);
    });
  }
}
