namespace TutorBot.Domain;

public class Student
{
  public Student(string gitHubUsername, string lastName, string firstName, string matNr, int groupNr)
  {
    GitHubUsername = gitHubUsername;
    LastName = lastName;
    FirstName = firstName;
    MatNr = matNr;
    GroupNr = groupNr;
  }

  public string GitHubUsername { get; init; }
  public string LastName { get; init; }
  public string FirstName { get; init; }
  public string MatNr { get; init; }
  public int GroupNr { get; init; }

  public virtual string FullName => $"{LastName} {FirstName}";
}
