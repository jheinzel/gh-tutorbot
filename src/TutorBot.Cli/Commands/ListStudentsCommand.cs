using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Domain;

namespace TutorBot.Commands;

internal class ListStudentsCommand : Command
{
  private readonly Option<int?> groupOption = new("--group", "filter group");

  private async Task HandleAsync(int? group)
  {
    var printer = new TablePrinter();
    printer.AddRow("LASTNAME", "FIRSTNAME", "STUD.ID", "GITHUBNAME", "GROUPNR.");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);

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

  public ListStudentsCommand() :
  base("list-students", "List all students")
  {
    groupOption.AddAlias("-g");
    groupOption.SetDefaultValue(null);
    AddOption(groupOption);

    this.AddAlias("lstud");

    this.SetHandler(HandleAsync, groupOption);
  }
}

