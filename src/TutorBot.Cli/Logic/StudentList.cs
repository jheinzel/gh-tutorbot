using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Logic;

public class StudentList : IEnumerable<Student>
{
  private readonly IDictionary<string, Student> students = new Dictionary<string, Student>();

  public static async Task<StudentList> FromRoster(string filePath)
  {
    try
    {
      return await FromRoster(File.OpenRead(filePath));
    }
    catch (FileNotFoundException)
    {
      throw new RosterFileException($"Roster file \"{filePath}\" not found");
    }
  }

  public static async Task<StudentList> FromRoster(Stream rosterStream)
  {
    var studentList = new StudentList();

    await foreach (List<string> line in CsvParser.Parse(rosterStream, ignoreFirstLine: true))
    {
      if (line.Count < 3)
      {
        throw new RosterFormatException($"Invalid roster line: \"{line}\"");
      }

      var githubUsername = line[1];

      if (string.IsNullOrEmpty(githubUsername)) // ignore unlinked students
      {
        continue;
      }

      var identifier = line[0]; // e.g. "Doe John (G9/S999999999)"
      Match match = Regex.Match(identifier, Constants.STUDENT_DATA_PATTERN);

      if (match.Success)
      {
        if (!studentList.students.TryAdd(githubUsername,
          new Student
          (
            firstName: match.Groups["FirstName"].Value,
            lastName: match.Groups["LastName"].Value,
            matNr: match.Groups["MatNr"].Value,
            groupNr: int.Parse(match.Groups["GroupNr"].Value),
            gitHubUsername: githubUsername
          )))
        {
          throw new RosterFileException($"Duplicate GitHub username \"{githubUsername}\" in roster file");
        }
      }
      else
      {
        throw new RosterFormatException($"Invalid student data format in roster line: \"{line}\"");
      }
    }

    return studentList;
  }

  public IEnumerator<Student> GetEnumerator()
  {
    return students.Values.GetEnumerator();
  }

  public bool TryGetValue(string gitHubUserName, [MaybeNullWhen(false)] out Student student)
  {
    return students.TryGetValue(gitHubUserName, out student);
  }

  public bool Contains(string gitHubUserName)
  {
    return students.ContainsKey(gitHubUserName);
  }

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
