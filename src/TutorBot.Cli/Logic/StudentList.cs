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
      throw new RosterFileNotFoundException($"Roster file \"{filePath}\" not found");
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

      var identifier = line[0]; // e.g. "Doe John (G9/S999999999)"
      var githubUsername = line[1];

      Match match = Regex.Match(identifier, Constants.STUDENT_DATA_PATTERN);

      if (match.Success)
      {
        studentList.students.Add(githubUsername, new Student
        (
          firstName: match.Groups["FirstName"].Value,
          lastName: match.Groups["LastName"].Value,
          matNr: match.Groups["MatNr"].Value,
          groupNr: int.Parse(match.Groups["GroupNr"].Value),
          gitHubUsername: githubUsername
        ));
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

  public bool TryGetValue(string matNr, [MaybeNullWhen(false)] out Student student)
  {
    return students.TryGetValue(matNr, out student);
  }

  IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
