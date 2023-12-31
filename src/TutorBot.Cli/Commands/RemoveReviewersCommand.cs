﻿using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Domain;
using TutorBot.Utility;
using TutorBot.Infrastructure;

namespace TutorBot.Commands;

internal class RemoveReviewersCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

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

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progressLoading = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progressLoading);
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

