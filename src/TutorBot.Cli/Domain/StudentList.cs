using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TutorBot.Domain.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Domain;

public interface IStudentList
{
  IReadOnlyList<Student> LinkedStudents { get; }
  IReadOnlyList<Student> UnlinkedStudents { get; }
  bool Contains(string gitHubUserName);
  bool TryGetValue(string gitHubUserName, [MaybeNullWhen(false)] out Student student);
}

public class StudentList : IStudentList
{
  private readonly IReadOnlyDictionary<string, Student> students = new Dictionary<string, Student>();
  private readonly IReadOnlyList<Student> unlinkedStudents = new List<Student>();

  private static IDictionary<string, Student> ToDictionary(IEnumerable<Student> students)
  {
    var dictionary = new Dictionary<string, Student>();
    foreach (var student in students)
    {
      dictionary.Add(student.GitHubUsername, student);
    }

    return dictionary;
  }

  public StudentList(IEnumerable<Student> students, IEnumerable<Student>? unlinkedStudents = null) : 
    this(ToDictionary(students), unlinkedStudents)
  {
  }

  private StudentList(IDictionary<string, Student> students, IEnumerable<Student>? unlinkedStudents = null)
  {
    this.students = students.AsReadOnly();
    this.unlinkedStudents = (unlinkedStudents ?? Enumerable.Empty<Student>()).ToList();
  }

  public static async Task<IStudentList> FromRoster(string filePath)
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

  public static async Task<IStudentList> FromRoster(Stream rosterStream)
  {
    var students = new Dictionary<string, Student>();
    var unlinkedStudents = new List<Student>();

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
          unlinkedStudents.Add(newStudent);
        }
        else if (!students.TryAdd(githubUsername, newStudent))
        {
          throw new RosterFileException($"Duplicate GitHub username \"{githubUsername}\" in roster file");
        }
      }
      else
      {
        throw new RosterFormatException($"Invalid student data format in roster line: \"{line}\"");
      }
    }

    return new StudentList(students, unlinkedStudents);
  }

  public IReadOnlyList<Student> LinkedStudents => students.Values.ToList().AsReadOnly();

  public IReadOnlyList<Student> UnlinkedStudents => unlinkedStudents;

  public bool TryGetValue(string gitHubUserName, [MaybeNullWhen(false)] out Student student)
  {
    return students.TryGetValue(gitHubUserName, out student);
  }

  public bool Contains(string gitHubUserName)
  {
    return students.ContainsKey(gitHubUserName);
  }

}
