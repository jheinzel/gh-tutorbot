using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListClassroomsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Option<string> orgOption = new("--org") { Description = "GitHub organization", Aliases = { "-o" } };

  private async Task HandleAsync(string org)
  {
    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "URL");

    try
    {
      var classrooms = await client.Classroom.GetAll(org);
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

  public ListClassroomsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
  base("list-classrooms", "List all classrooms")
  {
    this.client = client;
    this.configuration = configuration;

    orgOption.DefaultValueFactory = _ => this.configuration.DefaultOrganization;
    Options.Add(orgOption);

    Aliases.Add("lc");

    SetAction(async parsedResult =>
    {
      var org = parsedResult.GetRequiredValue(orgOption);
      await HandleAsync(org);
    });
  }
}

