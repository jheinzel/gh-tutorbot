using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;


internal class AssignReviewersCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");
  private readonly Option<bool> forceOption = new("--force", "force assignment although there are unlinked submissions");
  private readonly Option<bool> dryRunOption = new("--dry-run", "sumulate the execution of the command");

  private async Task HandleAsync(string assignmentName, string classroomName, bool dryRun, bool force)
  {
    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName, loadAssessments: true);

      if (assignment.UnlinkedSubmissions.Count == 0 || force)
      {
        int maxLength = studentList.LinkedStudents.Max(s => s.FullName.Length);
        await assignment.AssignReviewers(dryRun, successAction: (owner, reviewer) => Console.WriteLine($"{owner.FullName.PadRight(maxLength, ' ')} <- {reviewer.FullName}"));
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

  public AssignReviewersCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) :
    base("assign-reviewers", "Assign reviewers to assignments randomly")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    dryRunOption.AddAlias("-dr");
    dryRunOption.SetDefaultValue(false);
    AddOption(dryRunOption);

    forceOption.AddAlias("-f");
    forceOption.SetDefaultValue(false);
    AddOption(forceOption);

    AddAlias("ar");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption, dryRunOption, forceOption);
  }
}

