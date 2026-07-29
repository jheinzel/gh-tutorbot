using System.CommandLine;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class AssignReviewersCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment slug" };
  private readonly Option<string> classroomOption = new("--classroom") { Description = "classroom name", Aliases = { "-c" } };
  private readonly Option<string> orgOption = new("--org") { Description = "GitHub organization", Aliases = { "-o" } };
  private readonly Option<bool> forceOption = new("--force") { Description = "force assignment although there are unlinked submissions", Aliases = { "-f" } };

  private bool UserAgreesToAssignReviewers(Assignment assignment)
  {
    var numUnlinked = assignment.UnlinkedSubmissions.Count;

    string prompt;
    if (numUnlinked > 0)
    {
      prompt = $"Assign proposed reviewers although there are {numUnlinked} unlinked submissions (y/N)? ";
    }
    else
    {
      prompt = $"Assign proposed reviewers to assignment \"{assignment.Name}\" (y/N)? ";
    }

    return UiHelper.GetUserInput(prompt, answerOptions: new[] { "y", "n" }, defaultAnswer: "n") == "y";
  }

  private async Task HandleAsync(string assignmentSlug, string classroomName, string org, bool force)
  {
    try
    {
      var studentList = await StudentList.FromGitHub(client, org, classroomName);
      var classroom = await client.Classroom.GetByName(classroomName, org);

      var progressLoading = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentSlug, ClassroomName: classroomName, Org: org, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, configuration, progressLoading);
      progressLoading.Dispose();

      if (assignment.UnlinkedSubmissions.Count == 0 || force)
      {
        var allSubmissions = assignment.Submissions.ToList();
        var proposedReviewers = assignment.FindReviewers();

        if (proposedReviewers.Count == 0)
        {
          if (allSubmissions.Count == 0)
          {
            Console.WriteLine($"No submissions found in assignment slug \"{assignmentSlug}\".");
          }
          else if (allSubmissions.All(s => s.Reviewers.Count > 0))
          {
            Console.WriteLine($"All submissions in assignment slug \"{assignmentSlug}\" have already reviewers assigned.");
          }
          else
          {
            Console.WriteLine($"No additional reviewer assignments could be proposed for assignment slug \"{assignmentSlug}\".");
          }
        }
        else
        {
          int maxLength = studentList.LinkedStudents.Max(s => s.FullName.Length);
          Console.WriteLine($"Proposed reviewers for assignment slug \"{assignmentSlug}\"");

          foreach (var (submission, reviewer) in proposedReviewers)
          {
            Console.WriteLine($"{submission.Owner.FullName.PadRight(maxLength, ' ')} <- {reviewer.FullName}");
          }

          if (UserAgreesToAssignReviewers(assignment))
          {
            using var progressAssigning = new ProgressBar("Assigning reviewers");
            await assignment.AssignReviewers(proposedReviewers, progressAssigning);
          }
        }
      }
      else
      {
        var unlinkedSubmissions = assignment.UnlinkedSubmissions.Select(s => s.RepositoryName).ToStringWithSeparator();
        Console.Error.WriteRedLine($"The following submissions are not linked: {unlinkedSubmissions}");
        Console.Error.WriteRedLine("Use --force to ignore unlinked submissions and force assigning of reviewers");
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public AssignReviewersCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("assign-reviewers", "Assign reviewers to assignments randomly")
  {
    this.client = client;
    this.configuration = configuration;

    Add(assignmentArgument);

    classroomOption.DefaultValueFactory = _ => configuration.DefaultClassroom;
    Options.Add(classroomOption);

    orgOption.DefaultValueFactory = _ => configuration.DefaultOrganization;
    Options.Add(orgOption);

    forceOption.DefaultValueFactory = _ => false;
    Options.Add(forceOption);

    Aliases.Add("ar");

    SetAction(async parsedResult =>
    {
      var assignmentSlug = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetValue(classroomOption);
      var org = parsedResult.GetRequiredValue(orgOption);
      var force = parsedResult.GetValue(forceOption);
      await HandleAsync(assignmentSlug, classroomName!, org, force);
    });
  }
}

