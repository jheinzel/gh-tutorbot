using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListAssignmentsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Option<string> classroomOption = new("--classroom") { Description = "classroom name", Aliases = { "-c" } };

  private async Task HandleAsync(string classroomName)
  {
    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "DEADLINE", "SUBM.");

    try
    {
      var classroom = await client.Classroom.GetByName(classroomName);

      var assignments = await client.Classroom.Assignment.GetAll(classroom.Id);
      foreach (var assignment in assignments)
      {
        var deadLineStr = assignment.Deadline is null ? "-" : assignment.Deadline?.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        printer.AddRow(assignment.Id.ToString(), assignment.Title, deadLineStr, assignment.Accepted.ToString());
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListAssignmentsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) : 
    base("list-assignments", "List all assignments of a classroom")
  {
    this.client = client;
    this.configuration = configuration;

    classroomOption.DefaultValueFactory = _ => configuration.DefaultClassroom;
    Options.Add(classroomOption);

    Aliases.Add("la");

    SetAction(async parsedResult =>
    {
      var classroomName = parsedResult.GetRequiredValue(classroomOption);
      await HandleAsync(classroomName);
    });
  }
}

