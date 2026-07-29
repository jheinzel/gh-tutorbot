using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Domain;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListStudentsCommand : ClassroomCommand
{
  private readonly Option<int?> groupOption = new("--group") { Description = "filter group", Aliases = { "-g" } };

  private async Task HandleAsync(string? classroomName, string? org, int? group)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    var printer = new TablePrinter();
    printer.AddRow("LASTNAME", "FIRSTNAME", "STUD.ID", "GITHUBNAME", "GROUPNR.");

    try
    {
      var studentList = await StudentList.FromGitHub(Client, org, classroomName);

      var filteredlinkedStudents = studentList.LinkedStudents;
      if (group is not null)
      {
        filteredlinkedStudents = filteredlinkedStudents.Where(s => s.GroupNr == group).ToList();
      }
      foreach (var student in filteredlinkedStudents)
      {
        printer.AddRow(student.LastName, student.FirstName, student.MatNr, student.GitHubUsername, student.GroupNr.ToString());
      }

      var filteredUnLinkedStudents = studentList.UnlinkedStudents;
      if (group is not null)
      {
        filteredUnLinkedStudents = filteredUnLinkedStudents.Where(s => s.GroupNr == group).ToList();
      }
      foreach (var student in filteredUnLinkedStudents)
      {
        printer.AddRow(student.LastName, student.FirstName, student.MatNr, "-", student.GroupNr.ToString());
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListStudentsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("list-students", "List all students", client, configuration)
  {
    SetupCommonOptionDefaults();

    Options.Add(ClassroomOption);
    Options.Add(OrgOption);

    groupOption.DefaultValueFactory = _ => null;
    Options.Add(groupOption);

    Aliases.Add("lstud");

    SetAction(async parsedResult =>
    {
      var classroomName = parsedResult.GetValue(ClassroomOption);
      var org = parsedResult.GetValue(OrgOption);
      var group = parsedResult.GetValue(groupOption);
      await HandleAsync(classroomName, org, group);
    });
  }
}
