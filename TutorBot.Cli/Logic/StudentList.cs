using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Logic;

public class StudentList : IEnumerable<Student>
{
  private readonly IDictionary<string, Student> students = new Dictionary<string, Student>();

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
        {
          FirstName = match.Groups["FirstName"].Value,
          LastName = match.Groups["LastName"].Value,
          MatNr = match.Groups["MatNr"].Value,
          GroupNr = int.Parse(match.Groups["GroupNr"].Value),
          GitHubUsername = githubUsername
        });
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
