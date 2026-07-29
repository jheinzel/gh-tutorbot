using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListClassroomsCommand : ClassroomCommand
{
  private async Task HandleAsync(string? org)
  {
    ValidateOrg(ref org);

    var printer = new TablePrinter();
    printer.AddRow("ID", "NAME", "URL");

    try
    {
      var classrooms = await Client.Classroom.GetAll(org);
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
    base("list-classrooms", "List all classrooms", client, configuration)
  {
    OrgOption.DefaultValueFactory = _ => Configuration.DefaultOrganization;
    Options.Add(OrgOption);

    Aliases.Add("lc");

    SetAction(async parsedResult =>
    {
      var org = parsedResult.GetValue(OrgOption);
      await HandleAsync(org);
    });
  }
}
