using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Octokit;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
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
      if (line.Count < 7)
      {
        throw new RosterFormatException($"Invalid roster line: \"{string.Join(",", line)}\"");
      }

      var role = line[6].Trim();
      if (role != "student")
      {
        continue;
      }

      var username = line[0].Trim();
      var firstName = line[1].Trim();
      var lastName = line[2].Trim();
      var email = line[3].Trim();
      var section = line[4].Trim();

      var emailMatch = Regex.Match(email, Constants.MATNR_FROM_EMAIL_PATTERN);
      if (!emailMatch.Success)
      {
        throw new RosterFormatException($"Invalid email in roster line: \"{string.Join(",", line)}\"");
      }
      var matNr = emailMatch.Groups["MatNr"].Value;

      var sectionNr = section.StartsWith('G') ? section[1..] : section;
      if (!int.TryParse(sectionNr, out var groupNr))
      {
        throw new RosterFormatException($"Invalid section/group in roster line: \"{string.Join(",", line)}\"");
      }

      var newStudent = new Student
      (
        gitHubUsername: username,
        lastName: lastName,
        firstName: firstName,
        matNr: matNr,
        groupNr: groupNr
      );

      if (string.IsNullOrEmpty(username))
      {
        unlinkedStudents.Add(newStudent);
      }
      else if (!students.TryAdd(username, newStudent))
      {
        throw new RosterFileException($"Duplicate GitHub username \"{username}\" in roster file");
      }
    }

    return new StudentList(students, unlinkedStudents);
  }

  public static async Task<IStudentList> FromGitHub(IGitHubClassroomClient client, string org, string classroomName)
  {
    try
    {
      var repository = await client.Repository.Get(org, Constants.CLASSROOM_METADATA_REPO_NAME);
      var contentList = await client.Repository.Content.GetAllContents(repository.Id, $"{classroomName}/roster.csv");
      var rosterContent = contentList.Single().Content;

      using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rosterContent));
      return await FromRoster(stream);
    }
    catch (NotFoundException)
    {
      throw new DomainException($"Error: Could not find roster at \"https://github.com/{org}/{Constants.CLASSROOM_METADATA_REPO_NAME}/blob/HEAD/{classroomName}/roster.csv\".");
    }
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
