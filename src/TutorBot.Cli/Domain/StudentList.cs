using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TutorBot.Domain.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Domain;

public class StudentList
{
  private readonly IDictionary<string, Student> students = new Dictionary<string, Student>();
  private readonly IList<Student> unlinkedStudents = new List<Student>();

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

      var identifier = line[0]; // e.g. "Doe John (G9/S999999999)"
      Match match = Regex.Match(identifier, Constants.STUDENT_DATA_PATTERN);

      if (match.Success)
      {
        var newStudent = new Student
          (
            firstName: match.Groups["FirstName"].Value,
            lastName: match.Groups["LastName"].Value,
            matNr: match.Groups["MatNr"].Value,
            groupNr: int.Parse(match.Groups["GroupNr"].Value),
            gitHubUsername: githubUsername
          );

        if (string.IsNullOrEmpty(githubUsername))
        {
          studentList.unlinkedStudents.Add(newStudent);
        }
        else if (!studentList.students.TryAdd(githubUsername, newStudent))
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

  public IEnumerable<Student> LinkedStudents => students.Values;

  public IEnumerable<Student> UnlinkedStudents => unlinkedStudents;

  public bool TryGetValue(string gitHubUserName, [MaybeNullWhen(false)] out Student student)
  {
    return students.TryGetValue(gitHubUserName, out student);
  }

  public bool Contains(string gitHubUserName)
  {
    return students.ContainsKey(gitHubUserName);
  }

}
