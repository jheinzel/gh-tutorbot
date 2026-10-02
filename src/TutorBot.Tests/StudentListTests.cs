using FluentAssertions;
using TutorBot.Domain;

namespace TutorBot.Tests;

public class StudentListTests
{
  [Fact]
  public async Task FromRoster_WithEmptyRole_ShouldTreatRowAsStudent()
  {
    // Arrange
    var csvData = "username,first_name,last_name,email,section,github_id,role\n"
      + "gh-mayr,Franz,Mayr,S2110307001@students.fh-hagenberg.at,G1,1234,\n"
      + "gh-admin,Ada,Admin,S2110307999@students.fh-hagenberg.at,G9,9999,teacher";
    await using var rosterStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    // Act
    var studentList = await StudentList.FromRoster(rosterStream);

    // Assert
    studentList.LinkedStudents.Should().ContainSingle();

    var student = studentList.LinkedStudents[0];
    student.GitHubUsername.Should().Be("gh-mayr");
    student.FirstName.Should().Be("Franz");
    student.LastName.Should().Be("Mayr");
    student.MatNr.Should().Be("S2110307001");
    student.GroupNr.Should().Be(1);
  }

  [Fact]
  public async Task FromRoster_WithEmptyGitHubUsername_ShouldAddUnlinkedStudent()
  {
    // Arrange
    var csvData = "username,first_name,last_name,email,section,github_id,role\n"
      + ",Franz,Mayr,S2110307001@students.fh-hagenberg.at,G1,1234,";
    await using var rosterStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    // Act
    var studentList = await StudentList.FromRoster(rosterStream);

    // Assert
    studentList.LinkedStudents.Should().BeEmpty();
    studentList.UnlinkedStudents.Should().ContainSingle();

    var student = studentList.UnlinkedStudents[0];
    student.GitHubUsername.Should().BeEmpty();
    student.FirstName.Should().Be("Franz");
    student.LastName.Should().Be("Mayr");
    student.MatNr.Should().Be("S2110307001");
    student.GroupNr.Should().Be(1);
  }

  [Fact]
  public async Task FromRoster_WithExplicitStudentRole_ShouldParseStudentCorrectly()
  {
    // Arrange
    var csvData = "username,first_name,last_name,email,section,github_id,role\n"
      + "gh-huber,Susi,Huber,S2110307002@students.fh-hagenberg.at,G2,2345,student\n"
      + "gh-tutor,Tom,Tutor,S2110307888@students.fh-hagenberg.at,G8,8888,tutor";
    await using var rosterStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData));

    // Act
    var studentList = await StudentList.FromRoster(rosterStream);

    // Assert
    studentList.LinkedStudents.Should().ContainSingle();
    studentList.UnlinkedStudents.Should().BeEmpty();

    var student = studentList.LinkedStudents[0];
    student.GitHubUsername.Should().Be("gh-huber");
    student.FirstName.Should().Be("Susi");
    student.LastName.Should().Be("Huber");
    student.MatNr.Should().Be("S2110307002");
    student.GroupNr.Should().Be(2);
  }
}
