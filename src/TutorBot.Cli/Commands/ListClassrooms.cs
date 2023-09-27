using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Commands;

internal class ListClassroomsCommand : Command
{
  private readonly IGitHubClassroomClient client;

  private async Task HandleAsync()
  {
    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "URL");

    try
    {
      var classrooms = await client.Classroom.GetAll();
      foreach (var classroom in classrooms)
      {
        printer.AddRow(classroom.Id.ToString(), classroom.Name, classroom.Url);
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListClassroomsCommand(IGitHubClassroomClient client, ILogger<ListSubmissionsCommand> logger) :
  base("list-classrooms", "List all classrooms")
  {
    this.client = client;

    AddAlias("lc");

    this.SetHandler(HandleAsync);
  }
}

