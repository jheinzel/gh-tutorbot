using System.CommandLine;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class RemoveReviewersCommand : ClassroomCommand
{
  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment slug" };

  private bool UserAgreesToRemoveReviewers(Assignment assignment)
  {
    var numWithAssignedReviewers = assignment.Submissions.Count(s => s.Reviewers.Count > 0);
    var numValid = assignment.Submissions.Count(s => s.Assessment.IsValid());
    var numUnlinked = assignment.UnlinkedSubmissions.Count;

    if (numWithAssignedReviewers > 0)
    {
      var prompt = $"Remove reviewers from {numWithAssignedReviewers} submissions in assignment \"{assignment.Name}\"? (y/N)? ";
      return UiHelper.GetUserInput(prompt, answerOptions: new[] { "y", "n" }, defaultAnswer: "n") == "y";
    }
    else
    {
      Console.WriteLine($"No reviewers assigned to submissions in assignment \"{assignment.Name}\".");
      return false;
    }
  }

  private async Task HandleAsync(string assignmentSlug, string? classroomName, string? org)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    try
    {
      var studentList = await StudentList.FromGitHub(Client, org, classroomName);
      var classroom = await Client.Classroom.GetByName(classroomName, org);

      var progressLoading = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentSlug, ClassroomName: classroomName, Org: org);
      var assignment = await Assignment.FromGitHub(Client, studentList, parameters, Configuration, progressLoading);
      progressLoading.Dispose();

      if (UserAgreesToRemoveReviewers(assignment))
      {
        using var progressRemoving = new ProgressBar("Removing Reviewers");
        await assignment.RemoveReviewers(progressRemoving);
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public RemoveReviewersCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) : 
    base("remove-reviewers", "Remove reviewers from assignments", client, configuration)
  {
    SetupCommonOptionDefaults();

    Add(assignmentArgument);
    Options.Add(ClassroomOption);
    Options.Add(OrgOption);

    Aliases.Add("rr");

    SetAction(async parsedResult =>
    {
      var assignmentSlug = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetValue(ClassroomOption);
      var org = parsedResult.GetValue(OrgOption);
      await HandleAsync(assignmentSlug, classroomName, org);
    });
  }
}
