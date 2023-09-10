using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class CloneAssignmentCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");
  private readonly Option<string> directoryOption = new("--directory", "directory repositories will be cloned to");

  private async Task HandleAsync(string assignmentName, string classroomName, string? directory)
  {
    try
    {
      var studentList = await StudentList.FromRoster(File.OpenRead(Constants.ROSTER_FILE_PATH));
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName);
      
      await assignment.CloneRepositories(directory ?? assignment.Name, 
        successAction: (repoName) => Console.WriteLine($"Cloned repository \"{repoName}\""),
        failureAction: (repoName, errorMessage, _) => 
                         Console.Error.WriteLine($"Problems cloning repository \"{repoName}\":\n{errorMessage.Trim().Indent(2)}"));
    }
    catch (Exception ex) when (ex is LogicException || ex is InfrastrucureException)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.Error.WriteLine($"{ex.Message}");
      Console.ResetColor();
    }
  }

  public CloneAssignmentCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) : 
    base("clone-assignment", "Clone all repositories of an assignment")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    directoryOption.AddAlias("-d");
    directoryOption.SetDefaultValue(null);
    AddOption(directoryOption);

    AddAlias("ca");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption, directoryOption);
  }
}

