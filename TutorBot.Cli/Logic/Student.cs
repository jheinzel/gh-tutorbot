using System.Text.RegularExpressions;

namespace TutorBot.Logic;

public class Student
{
  public required string GitHubUsername { get; init; }
  public required string LastName { get; init; }
  public required string FirstName { get; init; }
  public required string MatNr { get; init; }
  public required int GroupNr { get; init; }

  public string FullName => $"{LastName} {FirstName} ";

}
