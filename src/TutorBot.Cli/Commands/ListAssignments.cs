using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListAssignmentsCommand : ClassroomCommand
{
  private async Task HandleAsync(string? classroomName, string? org)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    var printer = new TablePrinter();
    printer.AddRow("SLUG", "NAME", "DEADLINE", "SUBM.");

    try
    {
      var assignments = await Client.Classroom.Assignment.GetAll(org, classroomName);
      foreach (var assignment in assignments)
      {
        var deadLineStr = assignment.Deadline is null ? "-" : assignment.Deadline?.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        printer.AddRow(assignment.Slug, assignment.Title, deadLineStr, assignment.Accepted.ToString());
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListAssignmentsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) : 
    base("list-assignments", "List all assignments of a classroom", client, configuration)
  {
    SetupCommonOptionDefaults();
    Options.Add(ClassroomOption);
    Options.Add(OrgOption);

    Aliases.Add("la");

    SetAction(async parsedResult =>
    {
      var classroomName = parsedResult.GetValue(ClassroomOption);
      var org = parsedResult.GetValue(OrgOption);
      await HandleAsync(classroomName, org);
    });
  }
}

