using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TutorBot.Tests;

public class AssignmentTests
{
  private readonly IGitHubClassroomClient client;
  private readonly IClassroomsClient classroomsClient;
  private readonly IAssignmentsClient assignmentClient;
  private readonly ISubmissionsClient submissionsClient;
  private readonly IStudentList students;

  public AssignmentTests()
  {
    client = Substitute.For<IGitHubClassroomClient>();

    classroomsClient = Substitute.For<IClassroomsClient>();
    client.Classroom.Returns(classroomsClient);

    assignmentClient = Substitute.For<IAssignmentsClient>();
    classroomsClient.Assignment.Returns(assignmentClient);

    submissionsClient = Substitute.For<ISubmissionsClient>();
    classroomsClient.Submissions.Returns(submissionsClient);

    students = new StudentList(new List<Student> { new Student("gh-mayr", "Mayr", "Franz", "S2110307001", 1) });
  }

  [Fact]
  public async Task Submission_With_No_Repository_Throws_Exception()
  {
    assignmentClient.GetByName(1, "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1).ToString("o") }));

    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>(new List<SubmissionDto> { new SubmissionDto { Id = 100 } }));

    var parameters = new AssigmentParameters(1, "ue01", LoadAssessments: false);
    var fromGitHubAction = async () => await Assignment.FromGitHub(client, students, parameters);

    await fromGitHubAction.Should().ThrowAsync<SubmissionException>();
  }
}