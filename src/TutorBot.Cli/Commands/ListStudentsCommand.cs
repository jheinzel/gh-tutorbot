using System.CommandLine;
using TutorBot.Infrastructure;
using TutorBot.Logic;

namespace TutorBot.Commands;

internal class ListStudentsCommand : Command
{
  private async Task HandleAsync()
  {
    var printer = new TablePrinter();
    printer.AddRow("LASTNAME", "FIRSTNAME", "STUD.ID", "GITHUBNAME", "GROUPNR.");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);

      foreach (var student in studentList.LinkedStudents)
      {
        printer.AddRow(student.LastName, student.FirstName, student.MatNr, student.GitHubUsername, student.GroupNr.ToString());
      }

      foreach (var student in studentList.UnlinkedStudents)
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
    this.AddAlias("lstud");

    this.SetHandler(HandleAsync);
  }
}

