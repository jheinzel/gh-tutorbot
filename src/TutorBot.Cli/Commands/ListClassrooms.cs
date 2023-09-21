using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;

namespace TutorBot.Commands;

internal class ListClassroomsCommand : Command
{
  private readonly IGitHubClient client;

  private async Task HandleAsync()
  {
    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "URL");

    try
    {
      var classrooms = await client.Classroom().GetAll();
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

  public ListClassroomsCommand(IGitHubClient client, ILogger<ListSubmissionsCommand> logger) :
  base("list-classrooms", "List all classrooms")
  {
    this.client = client;

    AddAlias("lc");

    this.SetHandler(HandleAsync);
  }
}

