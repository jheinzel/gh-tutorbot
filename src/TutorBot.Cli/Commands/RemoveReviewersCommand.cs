using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Domain;
using TutorBot.Utility;
using TutorBot.Infrastructure;

namespace TutorBot.Commands;

internal class RemoveReviewersCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private bool UserAgreesToRemoveReviewers(Assignment assignment)
  {
    var numWithAssignedReviewers = assignment.Submissions.Count(s => s.Reviewers.Count > 0);
    var numValid = assignment.Submissions.Count(s => s.Assessment.IsValid());
    var numUnlinked = assignment.UnlinkedSubmissions.Count;

    var prompt = $"Remove reviewers from {numWithAssignedReviewers} submissions? (y/N): ";

    return UiHelper.GetUserInput(prompt, answerOptions: new[] { "y", "n" }, defaultAnswer: "n") == "y";
  }

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);

      var progress = new ProgressBar();
      var parameters = new AssigmentParameters(classroom.Id, assignmentName);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();

      if (UserAgreesToRemoveReviewers(assignment))
      {
        await assignment.RemoveReviewers();
      }

      var assignments = await client.Classroom().Assignment.GetAll(classroom.Id);
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public RemoveReviewersCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) : 
    base("remove-reviewers", "Remove reviewers from assignments")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    AddAlias("rr");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption);
  }
}

