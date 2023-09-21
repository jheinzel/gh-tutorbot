using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListAssignmentsCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string classroomName)
  {
    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "DEADLINE", "SUBM.");

    try
    {
      var classroom = await client.Classroom().GetByName(classroomName);

      var assignments = await client.Classroom().Assignment.GetAll(classroom.Id);
      foreach (var assignment in assignments)
      {
        printer.AddRow(assignment.Id.ToString(), assignment.Title, assignment.Deadline, assignment.Accepted.ToString());
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListAssignmentsCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) : 
    base("list-assignments", "List all assignments of a classroom")
  {
    this.client = client;
    this.configuration = configuration;

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    AddAlias("la");

    this.SetHandler(HandleAsync, classroomOption);
  }
}

