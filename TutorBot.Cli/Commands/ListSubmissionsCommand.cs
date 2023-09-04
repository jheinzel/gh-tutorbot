using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListSubmissionsCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "Assignment name");
  private readonly Option<string> organizationOption = new("--organization", "Organization name");


  private async Task HandleAsync(string assignmentName, string organization)
  {
    Console.WriteLine($"{"REPOSITORY",-25} {"STUDENT",-20} {"MAT.NR.",-12} {"REVIEWER(S)"}");

    try
    {
      var studentList = await StudentList.FromRoster(File.OpenRead(Constants.ROSTER_FILE_PATH));
      var assignment = await Assignment.FromGitHub(client, studentList, organization, assignmentName);

      foreach (var submission in assignment.Submissions)
      {
        var reviewers = string.Join(", ", submission.Reviewers.Select(r => r.FullName));
        Console.WriteLine($"{submission.RepositoryName,-25} {submission.Student.FullName,-20} " +
                          $"{submission.Student.MatNr,-12} {reviewers}");
      }
    }
    catch (FileNotFoundException)
    {
      Console.Error.WriteLine($"Roster file \"{Constants.ROSTER_FILE_PATH}\" not found.");
    }
    catch (LogicException le)
    {
      Console.Error.WriteLine($"{le.Message}");
    }
  }

  public ListSubmissionsCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListSubmissionsCommand> logger) : 
    base("list-submissions", "List all submissions of an assignment")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    organizationOption.AddAlias("-o");
    organizationOption.SetDefaultValue(configuration.DefaultOrganization);
    AddOption(organizationOption);

    AddAlias("ls");

    this.SetHandler(HandleAsync, assignmentArgument, organizationOption);
  }
}

