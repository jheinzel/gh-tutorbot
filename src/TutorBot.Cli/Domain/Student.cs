using System;

namespace TutorBot.Domain;

public class Student : IEquatable<Student>
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

  public override bool Equals(object? obj)
  {
    return Equals(obj as Student);
  }

  public bool Equals(Student? other)
  {
    if (other == null)
    {
      return false;
    }

    if (ReferenceEquals(this, other))
    {
      return true;
    }

    return GitHubUsername == other.GitHubUsername;
  }

  public override int GetHashCode()
  {
    return GitHubUsername?.GetHashCode() ?? 0;
  }
}
