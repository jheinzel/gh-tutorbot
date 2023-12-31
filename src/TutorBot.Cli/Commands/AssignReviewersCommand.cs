﻿using System.CommandLine;
using Microsoft.Extensions.Logging;
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

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");
  private readonly Option<bool> forceOption = new("--force", "force assignment although there are unlinked submissions");

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

  private async Task HandleAsync(string assignmentName, string classroomName, bool force)
  {
    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progressLoading = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progressLoading);
      progressLoading.Dispose();

      if (assignment.UnlinkedSubmissions.Count == 0 || force)
      {
        var proposedReviewers = assignment.FindReviewers();
        if (proposedReviewers.Count == 0)
        {
          Console.WriteLine($"All submissions in assignment \"{assignmentName}\" have already reviewers assigned.");
        }
        else
        {
          int maxLength = studentList.LinkedStudents.Max(s => s.FullName.Length);
          Console.WriteLine($"Proposed reviewers for assignment \"{assignmentName}\"");

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

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    forceOption.AddAlias("-f");
    forceOption.SetDefaultValue(false);
    AddOption(forceOption);

    AddAlias("ar");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption, forceOption);
  }
}

