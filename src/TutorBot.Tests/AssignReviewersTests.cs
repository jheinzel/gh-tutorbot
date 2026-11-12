using System.Net;
using System.Text;
using FluentAssertions;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Tests;

public class AssignReviewersTests
{
  private readonly IGitHubClassroomClient client;
  private readonly IRepositoriesClient repositoriesClient;
  private readonly IRepositoryContentsClient repositoryContentsClient;
  private readonly IRepoCollaboratorsClient repoCollaboratorsClient;
  private readonly IClassroomsClient classroomsClient;
  private readonly IReviewerAssignmentsClient reviewerAssignmentsClient;
  private readonly IOrganizationsClient organizationsClient;
  private readonly ITeamsClient teamsClient;
  private readonly IIssuesClient issuesClient;
  private readonly IIssueCommentsClient issueCommentsClient;
  private readonly List<ReviewerAssignmentsFile> savedReviewerAssignments = [];

  const string assessmentString = """
      # Erfüllungsgrad

      Aufwand (in Stunden): 10.5

      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 100          | 100              | 100              | 100          |
      """;

  public AssignReviewersTests()
  {
    client = Substitute.For<IGitHubClassroomClient>();

    repositoriesClient = Substitute.For<IRepositoriesClient>();
    client.Repository.Returns(repositoriesClient);

    repoCollaboratorsClient = Substitute.For<IRepoCollaboratorsClient>();
    client.Repository.Collaborator.Returns(repoCollaboratorsClient);

    classroomsClient = Substitute.For<IClassroomsClient>();
    client.Classroom.Returns(classroomsClient);
    reviewerAssignmentsClient = Substitute.For<IReviewerAssignmentsClient>();
    classroomsClient.ReviewerAssignments.Returns(reviewerAssignmentsClient);
    reviewerAssignmentsClient.Save(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ReviewerAssignmentsFile>())
                              .Returns(call =>
                              {
                                savedReviewerAssignments.Add(call.ArgAt<ReviewerAssignmentsFile>(2));
                                return Task.CompletedTask;
                              });

    organizationsClient = Substitute.For<IOrganizationsClient>();
    client.Organization.Returns(organizationsClient);

    teamsClient = Substitute.For<ITeamsClient>();
    organizationsClient.Team.Returns(teamsClient);

    var createdTeams = new List<Team>();
    teamsClient.GetAll(Arg.Any<string>()).Returns(_ => Task.FromResult<IReadOnlyList<Team>>(createdTeams));
    teamsClient.Create(Arg.Any<string>(), Arg.Any<NewTeam>()).Returns(ci =>
    {
      var org = ci.ArgAt<string>(0);
      var newTeam = ci.ArgAt<NewTeam>(1);
      var permissions = new TeamRepositoryPermissions(false, false, false, false, false);
      var orgEntity = new Organization();
      var team = new Team("", "", 1, "", newTeam.Name, $"{newTeam.Name.ToLowerInvariant()}", "", TeamPrivacy.Closed, "", permissions, 0, 0, orgEntity, null, "");
      createdTeams.Add(team);
      return Task.FromResult(team);
    });

    var userClient = Substitute.For<IUsersClient>();
    client.User.Returns(userClient);
    var authenticatedUser = new User();
    typeof(User).GetProperty(nameof(User.Login))!.SetValue(authenticatedUser, "org-owner");
    userClient.Current().Returns(Task.FromResult(authenticatedUser));

    teamsClient.AddOrEditMembership(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<UpdateTeamMembership>())
               .Returns(Task.FromResult(new TeamMembershipDetails(TeamRole.Member, MembershipState.Active)));
    teamsClient.RemoveMembership(Arg.Any<long>(), Arg.Any<string>()).Returns(Task.FromResult(true));
    teamsClient.AddOrUpdateTeamRepositoryPermissions(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
               .Returns(Task.CompletedTask);
    teamsClient.Delete(Arg.Any<long>()).Returns(Task.CompletedTask);
    teamsClient.GetAllMembers(Arg.Any<long>()).Returns(Task.FromResult<IReadOnlyList<User>>([]));

    issuesClient = Substitute.For<IIssuesClient>();
    client.Issue.Returns(issuesClient);

    issueCommentsClient = Substitute.For<IIssueCommentsClient>();
    issuesClient.Comment.Returns(issueCommentsClient);
    issueCommentsClient.Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>())
                       .Returns(Task.FromResult<IssueComment>(null!));

    repositoryContentsClient = Substitute.For<IRepositoryContentsClient>();
    client.Repository.Content.Returns(repositoryContentsClient);
  }

  private static void AddContent(IRepositoryContentsClient contentClient, string assessmentString)
  {
    var encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(assessmentString));
    RepositoryContent reopContent = new("", "", "", 0, ContentType.File, "", "", "", "", "", encodedContent, "", "");
    contentClient.GetAllContents(Arg.Any<long>(), Arg.Any<string>()).Returns(Task.FromResult<IReadOnlyList<RepositoryContent>>([reopContent]));
  }

  [Fact]
  public void ReviewerAssignment_WithNoSubmissions_ShouldHaveNoReviewers()
  {
    var submissions = new List<Submission>();
    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    var reviewers = assignment.FindReviewers();
    reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task ReviewerAssignment_WithOneSubmission_ShouldHaveNoReviewers()
  {
    var students = CreateStudentList(1);
    var emptyReviewerList = new List<Reviewer>();

    var repository = CreateRepository(100, "repo1");

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>
    {
      new(client, repository, students[0], emptyReviewerList)
    };
    foreach (var s in submissions) await s.Assessment.Load(client, s.RepositoryId);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();

    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    var reviewers = assignment.FindReviewers();
    reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task ReviewerAssignment_WithTwoSubmission_ShouldBeCorrect()
  {
    var students = CreateStudentList(2);
    var emptyReviewerList = new List<Reviewer>();

    var repository = CreateRepository(100, "repo1");

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>
    {
      new(client, repository, students[0], emptyReviewerList),
      new(client, repository, students[1], emptyReviewerList)
    };
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();

    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    IList<(Submission Submission, Student Reviewer)> reviewers = assignment.FindReviewers().ToList();
    reviewers.Should().HaveCount(2);
    reviewers.Should().ContainSingle(r => ReferenceEquals(r.Submission, submissions[0]) && ReferenceEquals(r.Reviewer, students[1]));
    reviewers.Should().ContainSingle(r => ReferenceEquals(r.Submission, submissions[1]) && ReferenceEquals(r.Reviewer, students[0]));
  }

  [Theory]
  [InlineData(3)]
  [InlineData(10)]
  [InlineData(30)]
  public async Task ReviewerAssignment_WithNoPreAssignedReviewers_ShouldBeCorrect(int numSubmission)
  {
    var students = CreateStudentList(numSubmission);
    var emptyReviewerList = new List<Reviewer>();

    var repository = CreateRepository(100, "repo1");

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>();
    for (int i = 0; i < numSubmission; i++)
    {
      submissions.Add(new Submission(client, repository, students[i], emptyReviewerList));
    }
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);
   
    var reviewers = assignment.FindReviewers();

    ReviewerAssignmentShouldBeCorrect(reviewers, assignment.Submissions.Count);
  }

  [Fact]
  public async Task ReviewerAssignment_WithThreeSubmissionsAndOnePreAssignedReviewer_ShouldBeCorrect()
  {
    var students = CreateStudentList(3);
    var emptyReviewerList = new List<Reviewer>();

    var repository = CreateRepository(100, "repo1");

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>
    {
      new(client, repository, students[0], [new Reviewer(students[1])]),
      new(client, repository, students[1], emptyReviewerList),
      new(client, repository, students[2], emptyReviewerList)
    };
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    var reviewers = assignment.FindReviewers();
    foreach (var (submission, reviewer) in reviewers)
    {
      submission.Reviewers.Add(new Reviewer(reviewer));
    }

    ReviewerAssignmentShouldBeCorrect(assignment);
  }

  [Theory]
  [InlineData(5)]
  [InlineData(10)]
  [InlineData(30)]
  public async Task ReviewerAssignment_WithPreAssignedReviewers_ShouldBeCorrect(int numSubmissions)
  {
    var students = CreateStudentList(numSubmissions);
    var emptyReviewerList = new List<Reviewer>();

    var repository = CreateRepository(100, "repo1");

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>();
    for (int i = 0; i < numSubmissions; i++)
    {
      if (i % 2 == 0)
      {
        submissions.Add(new Submission(client, repository, students[i], emptyReviewerList));
      }
      else
      {
        submissions.Add(new Submission(client, repository, students[i], [new(students[i - 1])]));
      } 
    }
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    var reviewers = assignment.FindReviewers();
    foreach (var (submission, reviewer) in reviewers)
    {
      submission.Reviewers.Add(new Reviewer(reviewer));
    }

    ReviewerAssignmentShouldBeCorrect(assignment);
  }

  [Fact]
  public async Task AssignmentOfProposedReviewers_IsCorrectlyIntegratedIntoSubmissions()
  {
    var students = CreateStudentList(3);
    var emptyReviewerList = new List<Reviewer>();

    AddContent(repositoryContentsClient, assessmentString);

    var submissions = new List<Submission>();
    for (int i = 0; i < 3; i++)
    {
      var repository = CreateRepository(100 + i, $"repo{i + 1}");
      submissions.Add(new Submission(client, repository, students[i], emptyReviewerList));
    }
    foreach (var s in submissions) await s.Assessment.Load(client, s.RepositoryId);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client,
                                    "ue01",
                                    DateTime.Now.AddDays(1),
                                    submissions,
                                    unlinkedSubmissions,
                                    "swo3",
                                    "classroom",
                                    new ReviewerAssignmentsFile { AssignmentSlug = "ue01" })
    {
      Slug = "ue01"
    };

    var reviewers = new List<(Submission, Student)>
    {
      ( submissions[0], students[1] ),
      ( submissions[1], students[2] ),
      ( submissions[2], students[0] )
    };

    await assignment.AssignReviewers(reviewers);

    ReviewerAssignmentShouldBeCorrect(assignment);
    await teamsClient.Received(3).RemoveMembership(1, "org-owner");
    savedReviewerAssignments.Should().ContainSingle();
    savedReviewerAssignments[0].Repositories.Should().BeEquivalentTo(new Dictionary<string, List<string>>
    {
      ["swo3/repo1"] = ["gh-student-2"],
      ["swo3/repo2"] = ["gh-student-3"],
      ["swo3/repo3"] = ["gh-student-1"]
    });

    await assignment.RemoveReviewers();

    await teamsClient.Received(3).Delete(1);
    savedReviewerAssignments.Should().HaveCount(2);
    savedReviewerAssignments[1].Repositories.Should().BeEmpty();
    submissions.Should().OnlyContain(submission => submission.Reviewers.Count == 0);
  }

  [Fact]
  public async Task RemoveReviewers_CleansUnlinkedAndMissingRepositoriesAndCanBeRepeated()
  {
    var students = CreateStudentList(2);
    var submission = new Submission(client, CreateRepository(1, "repo1"), students[0], [new Reviewer(students[1])]);
    var unlinked = new UnlinkedSubmission(CreateRepository(2, "repo2"));
    var document = new ReviewerAssignmentsFile
    {
      AssignmentSlug = "ue01",
      Repositories = new(StringComparer.OrdinalIgnoreCase)
      {
        ["swo3/repo1"] = ["gh-student-2"],
        ["swo3/repo2"] = ["unknown-reviewer"],
        ["swo3/repo3"] = []
      }
    };
    var assignment = new Assignment(client, "ue01", null, [submission], [unlinked], "swo3", "classroom", document) { Slug = "ue01" };
    var teams = new[] { CreateTeam(1, "repo1-reviewers"), CreateTeam(2, "repo2-reviewers"), CreateTeam(3, "repo3-reviewers"), CreateTeam(4, "other-reviewers") };
    teamsClient.GetAll("swo3").Returns(Task.FromResult<IReadOnlyList<Team>>(teams), Task.FromResult<IReadOnlyList<Team>>([]));

    await assignment.RemoveReviewers();
    await assignment.RemoveReviewers();

    await teamsClient.Received(1).Delete(1);
    await teamsClient.Received(1).Delete(2);
    await teamsClient.Received(1).Delete(3);
    await teamsClient.DidNotReceive().Delete(4);
    await repoCollaboratorsClient.DidNotReceiveWithAnyArgs().Delete(default, default!);
    savedReviewerAssignments.Should().ContainSingle();
    savedReviewerAssignments[0].Repositories.Should().BeEmpty();
    submission.Reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task RemoveReviewers_WhenTeamDeletionFails_PreservesAssignmentsForRetry()
  {
    var students = CreateStudentList(2);
    var submission = new Submission(client, CreateRepository(1, "repo1"), students[0], [new Reviewer(students[1])]);
    var document = new ReviewerAssignmentsFile
    {
      AssignmentSlug = "ue01",
      Repositories = new(StringComparer.OrdinalIgnoreCase) { ["swo3/repo1"] = ["gh-student-2"] }
    };
    var assignment = new Assignment(client, "ue01", null, [submission], [], "swo3", "classroom", document) { Slug = "ue01" };
    teamsClient.GetAll("swo3").Returns(Task.FromResult<IReadOnlyList<Team>>([CreateTeam(1, "repo1-reviewers")]));
    teamsClient.Delete(1).Returns<Task>(_ => throw new InvalidOperationException("delete failed"));

    var act = () => assignment.RemoveReviewers();

    await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("delete failed");
    savedReviewerAssignments.Should().BeEmpty();
    submission.Reviewers.Should().ContainSingle();
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public async Task RemoveReviewers_WhenDeletionReturnsNotFound_ChecksWhetherTeamStillExists(bool teamStillExists)
  {
    var students = CreateStudentList(2);
    var submission = new Submission(client, CreateRepository(1, "repo1"), students[0], [new Reviewer(students[1])]);
    var document = new ReviewerAssignmentsFile
    {
      AssignmentSlug = "ue01",
      Repositories = new(StringComparer.OrdinalIgnoreCase) { ["swo3/repo1"] = ["gh-student-2"] }
    };
    var assignment = new Assignment(client, "ue01", null, [submission], [], "swo3", "classroom", document) { Slug = "ue01" };
    var team = CreateTeam(1, "repo1-reviewers");
    teamsClient.GetAll("swo3").Returns(Task.FromResult<IReadOnlyList<Team>>([team]),
                                       Task.FromResult<IReadOnlyList<Team>>(teamStillExists ? [team] : []));
    teamsClient.Delete(1).Returns<Task>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));

    var act = () => assignment.RemoveReviewers();

    if (teamStillExists)
    {
      await act.Should().ThrowAsync<NotFoundException>();
      savedReviewerAssignments.Should().BeEmpty();
      submission.Reviewers.Should().ContainSingle();
    }
    else
    {
      await act.Should().NotThrowAsync();
      savedReviewerAssignments.Should().ContainSingle().Which.Repositories.Should().BeEmpty();
      submission.Reviewers.Should().BeEmpty();
    }
  }

  private static Team CreateTeam(long id, string name)
  {
    var permissions = new TeamRepositoryPermissions(false, false, false, false, false);
    return new Team("", "", id, "", name, name, "", TeamPrivacy.Closed, "", permissions, 0, 0, new Organization(), null, "");
  }

  [Fact]
  public async Task AssignReviewers_WhenFeedbackPullRequestDoesNotExist_StillSavesReviewerAssignments()
  {
    issueCommentsClient.Create(Arg.Any<string>(), "repo2", Arg.Any<long>(), Arg.Any<string>())
                       .Returns<Task<IssueComment>>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));

    var students = CreateStudentList(3);
    var submissions = Enumerable.Range(0, 3)
      .Select(i => new Submission(client, CreateRepository(100 + i, $"repo{i + 1}"), students[i], []))
      .ToList();
    var assignment = new Assignment(client,
                                    "ue01",
                                    DateTime.Now.AddDays(1),
                                    submissions,
                                    [],
                                    "swo3",
                                    "classroom",
                                    new ReviewerAssignmentsFile { AssignmentSlug = "ue01" })
    {
      Slug = "ue01"
    };
    var reviewers = submissions.Select((submission, i) => (submission, students[(i + 1) % students.Count])).ToList();
    var originalError = Console.Error;
    using var warningOutput = new StringWriter();
    Console.SetError(warningOutput);

    try
    {
      await assignment.AssignReviewers(reviewers);
    }
    finally
    {
      Console.SetError(originalError);
    }

    ReviewerAssignmentShouldBeCorrect(assignment);
    savedReviewerAssignments.Should().ContainSingle();
    savedReviewerAssignments[0].Repositories.Should().HaveCount(3);
    warningOutput.ToString().Should().Contain($"Repository \"swo3/repo2\" for \"{students[1].FullName}\" has no feedback pull request.");
  }

  private static Repository CreateRepository(int id, string repoName)
  {
    return new Repository("", htmlUrl: $"https://{repoName}", "", "", "", "", "", "", id, "", owner: null, repoName, $"swo3/{repoName}", false, "", "", "", true, false, 0, 0, "", 0, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, permissions: null, null, null, null, false, false, false, false, false, 0, 0, false, false, false, false, 0, false, RepositoryVisibility.Private, [], null, null, null, new());
  }

  private static List<Student> CreateStudentList(int n)
  {
    var students = new List<Student>();
    for (int i = 1; i <= n; i++)
    {
      students.Add(new Student($"gh-student-{i}", $"first-{i}", $"student-{i}", $"S{i}", 1));
    }

    return students;
  }

  private static void ReviewerAssignmentShouldBeCorrect(IEnumerable<(Submission, Student)> reviewers, int expectedSize)
  {
    reviewers.Should().HaveCount(expectedSize);
    foreach (var (submission, reviewer) in reviewers)
    {
      reviewer.Should().NotBeNull();
      submission.Owner.Should().NotBe(reviewer);
    }
    reviewers.Select((s, r) => r).Should().OnlyHaveUniqueItems();
  }

  private static void ReviewerAssignmentShouldBeCorrect(Assignment assignment)
  {
    foreach (var submission in assignment.Submissions)
    {
      submission.Reviewers.Should().HaveCount(1);
      submission.Owner.Should().NotBe(submission.Reviewers[0]);
    }
    assignment.Submissions.SelectMany(s => s.Reviewers).Should().OnlyHaveUniqueItems();
  }
}