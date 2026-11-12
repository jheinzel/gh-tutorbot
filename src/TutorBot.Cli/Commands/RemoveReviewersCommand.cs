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
    var prompt = $"Remove reviewer assignments and reviewer teams for assignment \"{assignment.Name}\"? (y/N)? ";
    return UiHelper.GetUserInput(prompt, answerOptions: new[] { "y", "n" }, defaultAnswer: "n") == "y";
  }

  private async Task HandleAsync(string assignmentSlug, string? classroomName, string? org)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    try
    {
      var studentList = await StudentList.FromGitHub(Client, org, classroomName);
      var classroom = await Client.Classroom.GetByName(classroomName, org);

      var parameters = new AssigmentParameters(classroom.Id, assignmentSlug, ClassroomName: classroomName, Org: org, LoadReviewers: false);
      Assignment assignment;
      using (var progressLoading = new ProgressBar("Loading submissions"))
      {
        assignment = await Assignment.FromGitHub(Client, studentList, parameters, Configuration, progressLoading);
      }

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
