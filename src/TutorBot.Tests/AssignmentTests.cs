using FluentAssertions;
using Octokit;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Tests;

public class AssignmentTests
{
  private readonly IGitHubClassroomClient client;
  private readonly IRepositoriesClient repositoriesClient;
  private readonly IClassroomsClient classroomsClient;
  private readonly IAssignmentsClient assignmentClient;
  private readonly ISubmissionsClient submissionsClient;
  private readonly IRepoCollaboratorsClient collaboratorClient;
  private readonly IStudentList students;

  public AssignmentTests()
  {
    client = Substitute.For<IGitHubClassroomClient>();

    repositoriesClient = Substitute.For<IRepositoriesClient>();
    client.Repository.Returns(repositoriesClient);

    client.Repository.Get(100).Returns(CreateRepository(100, "repo1"));

    classroomsClient = Substitute.For<IClassroomsClient>();
    client.Classroom.Returns(classroomsClient);

    assignmentClient = Substitute.For<IAssignmentsClient>();
    classroomsClient.Assignment.Returns(assignmentClient);

    submissionsClient = Substitute.For<ISubmissionsClient>();
    classroomsClient.Submissions.Returns(submissionsClient);

    collaboratorClient = Substitute.For<IRepoCollaboratorsClient>();
    client.Repository.Collaborator.Returns(collaboratorClient);

    students = new StudentList(new List<Student> 
    {
      new Student("gh-mayr", "Mayr", "Franz", "S2110307001", 1),
      new Student("gh-huber", "Huber", "Susi", "S2110307002", 1)
    });
  }

  [Fact]
  public async Task Submission_WithNoRepository_ThrowsException()
  {
    assignmentClient.GetByName(1, "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    var submissionDto1 = new SubmissionDto { Id = 100, Students = new List<StudentDto> { new StudentDto { Id = 1, Login = "gh-mayr" } } };
    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>(new List<SubmissionDto> { submissionDto1 }));

    var parameters = new AssigmentParameters(1, "ue01", LoadAssessments: false);
    var fromGitHubAction = async () => await Assignment.FromGitHub(client, students, parameters);

    await fromGitHubAction.Should().ThrowAsync<SubmissionException>();
  }

  [Fact]
  public async Task SimpleAssignment_IsLoadedCorrectly()
  {
    var assignmentName = "ue01";
    assignmentClient.GetByName(1, assignmentName).Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    var studentDto1 = new StudentDto { Id = 1, Login = "gh-mayr" };
    var submissionDto1 = new SubmissionDto { Id = 100, Students = new List<StudentDto> { studentDto1 }, Repository = new RepositoryDto { Id = 100 } };
    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>(new List<SubmissionDto> { submissionDto1 }));

    collaboratorClient.GetAll(100).Returns(Task.FromResult<IReadOnlyList<Collaborator>>(new List<Collaborator> { }));

    var parameters = new AssigmentParameters(1, "ue01", LoadAssessments: false);
    var assignment = await Assignment.FromGitHub(client, students, parameters);

    var expectedOwner = students.LinkedStudents.Single(s => s.GitHubUsername == studentDto1.Login);

    assignment.Should().NotBeNull();
    assignment.Name.Should().Be(assignmentName);
    assignment.Submissions.Should().HaveCount(1);
    assignment.Submissions[0].Owner.GitHubUsername.Should().Be(studentDto1.Login);
    assignment.Submissions[0].Owner.LastName.Should().Be(expectedOwner.LastName);
    assignment.Submissions[0].Owner.FirstName.Should().Be(expectedOwner.FirstName);
    assignment.Submissions[0].Reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task SimpleAssignment_With_Reviewers_IsLoadedCorrectly()
  {
    var assignmentName = "ue01";
    assignmentClient.GetByName(1, assignmentName).Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    var studentDto1 = new StudentDto { Id = 1, Login = "gh-mayr" };
    var submissionDto1 = new SubmissionDto { Id = 100, Students = new List<StudentDto> { studentDto1 }, Repository = new RepositoryDto { Id = 100 } };
    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>(new List<SubmissionDto> { submissionDto1 }));

    var reviewerName = "gh-huber";
    var permissions1 = new CollaboratorPermissions(pull: false, triage: false, push: false, maintain: false, admin: false);
    var collaborator1 = new Collaborator("gh-huber", id: 2, "gh-huber@gmail.com", "Huber", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", false, permissions: permissions1, "read");
    collaboratorClient.GetAll(100).Returns(Task.FromResult<IReadOnlyList<Collaborator>>(new List<Collaborator> { collaborator1 }));

    var parameters = new AssigmentParameters(1, "ue01", LoadAssessments: false);
    var assignment = await Assignment.FromGitHub(client, students, parameters);

    var expectedOwner = students.LinkedStudents.Single(s => s.GitHubUsername == studentDto1.Login);
    var expectedReviewer = students.LinkedStudents.Single(s => s.GitHubUsername == reviewerName);

    assignment.Should().NotBeNull();
    assignment.Name.Should().Be(assignmentName);
    assignment.Submissions.Should().HaveCount(1);
    assignment.Submissions[0].Reviewers.Should().HaveCount(1);
    assignment.Submissions[0].Reviewers[0].LastName.Should().Be(expectedReviewer.LastName);
    assignment.Submissions[0].Reviewers[0].FirstName.Should().Be(expectedReviewer.FirstName);
  }

  private Repository CreateRepository(int id, string repoName)
  {
    return new Repository("", htmlUrl: $"https://{repoName}", "", "", "", "", "", id, "", owner: null, repoName, $"swo3/{repoName}", false, "", "", "", true, false, 0, 0, "", 0, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, permissions: null, null, null, null, false, false, false, false, false, 0, 0, false, false, false, false, 0, false, RepositoryVisibility.Private, Enumerable.Empty<string>(), null, null, null);
  }
}