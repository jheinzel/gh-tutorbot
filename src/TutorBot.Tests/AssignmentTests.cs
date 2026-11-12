using System.Net;
using FluentAssertions;
using Octokit;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Tests;

public class AssignmentTests
{
  private readonly IGitHubClassroomClient client;
  private readonly IRepositoriesClient repositoriesClient;
  private readonly IClassroomsClient classroomsClient;
  private readonly IAssignmentsClient assignmentClient;
  private readonly IReviewerAssignmentsClient reviewerAssignmentsClient;
  private readonly ISubmissionsClient submissionsClient;
  private readonly IRepoCollaboratorsClient collaboratorClient;
  private readonly IOrganizationsClient organizationsClient;
  private readonly ITeamsClient teamsClient;
  private readonly ISearchClient searchClient;
  private readonly IStudentList students;

  public AssignmentTests()
  {
    client = Substitute.For<IGitHubClassroomClient>();

    repositoriesClient = Substitute.For<IRepositoriesClient>();
    client.Repository.Returns(repositoriesClient);

    classroomsClient = Substitute.For<IClassroomsClient>();
    client.Classroom.Returns(classroomsClient);

    reviewerAssignmentsClient = Substitute.For<IReviewerAssignmentsClient>();
    classroomsClient.ReviewerAssignments.Returns(reviewerAssignmentsClient);
    reviewerAssignmentsClient.Get(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                              .Returns(call => Task.FromResult(new ReviewerAssignmentsFile { AssignmentSlug = call.ArgAt<string>(2) }));

    assignmentClient = Substitute.For<IAssignmentsClient>();
    classroomsClient.Assignment.Returns(assignmentClient);

    submissionsClient = Substitute.For<ISubmissionsClient>();
    classroomsClient.Submissions.Returns(submissionsClient);

    collaboratorClient = Substitute.For<IRepoCollaboratorsClient>();
    client.Repository.Collaborator.Returns(collaboratorClient);

    organizationsClient = Substitute.For<IOrganizationsClient>();
    client.Organization.Returns(organizationsClient);

    teamsClient = Substitute.For<ITeamsClient>();
    organizationsClient.Team.Returns(teamsClient);

    teamsClient.GetAll(Arg.Any<string>()).Returns(Task.FromResult<IReadOnlyList<Team>>([]));
    teamsClient.GetAllMembers(Arg.Any<long>()).Returns(Task.FromResult<IReadOnlyList<User>>([]));

    searchClient = Substitute.For<ISearchClient>();
    client.Search.Returns(searchClient);

    students = new StudentList(
    [
      new Student("gh-mayr", "Mayr", "Franz", "S2110307001", 1),
      new Student("gh-huber", "Huber", "Susi", "S2110307002", 1)
    ]);
  }

  [Fact]
  public async Task Submission_WithNoRepository_ThrowsException()
  {
    assignmentClient.GetBySlug(10, "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "ue01", Slug = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    var submissionDto1 = new SubmissionDto { Id = 100, Students = [new() { Id = 1, Login = "gh-mayr" }] };
    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>([submissionDto1]));

    // Use classroomId only, no Org/ClassroomName to avoid SearchRepo call
    var parameters = new AssigmentParameters(10, "ue01", LoadAssessments: false);
    var fromGitHubAction = async () => await Assignment.FromGitHub(client, students, parameters);

    await fromGitHubAction.Should().ThrowAsync<SubmissionException>();
  }

  [Fact]
  public async Task SimpleAssignment_IsLoadedCorrectly()
  {
    repositoriesClient.Get(100).Returns(CreateRepository(100, "repo1"));

    var assignmentSlug = "ue01";
    assignmentClient.GetBySlug(10, assignmentSlug).Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "Uebung 1", Slug = assignmentSlug, Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    var studentDto1 = new StudentDto { Id = 1, Login = "gh-mayr" };
    var submissionDto1 = new SubmissionDto { Id = 100, Students = [studentDto1], Repository = new RepositoryDto { Id = 100 } };
    submissionsClient.GetAll(10).Returns(Task.FromResult<IReadOnlyList<SubmissionDto>>([submissionDto1]));

    collaboratorClient.GetAll(100).Returns(Task.FromResult<IReadOnlyList<Collaborator>>([]));

    // Use classroomId only, no Org/ClassroomName to avoid SearchRepo call
    var parameters = new AssigmentParameters(10, "ue01", LoadAssessments: false);
    var assignment = await Assignment.FromGitHub(client, students, parameters);

    var expectedOwner = students.LinkedStudents.Single(s => s.GitHubUsername == studentDto1.Login);

    assignment.Should().NotBeNull();
    assignment.Name.Should().Be("Uebung 1");
    assignment.Submissions.Should().HaveCount(1);
    assignment.Submissions[0].Owner.GitHubUsername.Should().Be(studentDto1.Login);
    assignment.Submissions[0].Owner.LastName.Should().Be(expectedOwner.LastName);
    assignment.Submissions[0].Owner.FirstName.Should().Be(expectedOwner.FirstName);
    assignment.Submissions[0].Reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task AssignmentWithLowercaseRepositoryOwner_MatchesMixedCaseStudentUsername()
  {
    const string assignmentPrefix = "classroom-ue01-";
    var repo = CreateRepository(100, $"{assignmentPrefix}gh-mayr");
    repositoriesClient.Get(100).Returns(repo);
    assignmentClient.GetBySlug("swo3", "classroom", "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "Uebung 1", Slug = "ue01", Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));

    searchClient.SearchRepo(Arg.Any<SearchRepositoriesRequest>()).Returns(
      Task.FromResult(new SearchRepositoryResult(1, false, [repo])));
    collaboratorClient.GetAll(100).Returns(Task.FromResult<IReadOnlyList<Collaborator>>([]));

    var mixedCaseStudents = new StudentList(
    [
      new Student("GH-Mayr", "Mayr", "Franz", "S2110307001", 1)
    ]);
    var parameters = new AssigmentParameters(10, "ue01", ClassroomName: "classroom", Org: "swo3", LoadAssessments: false);

    var assignment = await Assignment.FromGitHub(client, mixedCaseStudents, parameters);

    assignment.Submissions.Should().ContainSingle();
    assignment.Submissions[0].Owner.GitHubUsername.Should().Be("GH-Mayr");
    assignment.UnlinkedSubmissions.Should().BeEmpty();
  }

  [Fact]
  public async Task SimpleAssignment_With_Reviewers_IsLoadedCorrectly()
  {
    var repository = CreateRepository(100, "classroom-ue01-gh-mayr");
    var assignmentSlug = "ue01";
    assignmentClient.GetBySlug("swo3", "classroom", assignmentSlug).Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "Uebung 1", Slug = assignmentSlug, Accepted = 1, Deadline = DateTime.Now.AddDays(1) }));
    searchClient.SearchRepo(Arg.Any<SearchRepositoriesRequest>()).Returns(
      Task.FromResult(new SearchRepositoryResult(1, false, [repository])));

    var reviewerName = "gh-huber";
    reviewerAssignmentsClient.Get("swo3", "classroom", assignmentSlug).Returns(
      Task.FromResult(new ReviewerAssignmentsFile
      {
        AssignmentSlug = assignmentSlug,
        Repositories = new(StringComparer.OrdinalIgnoreCase) { [repository.FullName] = [reviewerName] }
      }));

    var parameters = new AssigmentParameters(10, assignmentSlug, ClassroomName: "classroom", Org: "swo3", LoadAssessments: false);
    var assignment = await Assignment.FromGitHub(client, students, parameters);

    var expectedOwner = students.LinkedStudents.Single(s => s.GitHubUsername == "gh-mayr");
    var expectedReviewer = students.LinkedStudents.Single(s => s.GitHubUsername == reviewerName);

    assignment.Should().NotBeNull();
    assignment.Name.Should().Be("Uebung 1");
    assignment.Submissions.Should().ContainSingle();
    assignment.Submissions[0].Owner.GitHubUsername.Should().Be(expectedOwner.GitHubUsername);
    assignment.Submissions[0].Reviewers.Should().ContainSingle();
    assignment.Submissions[0].Reviewers[0].LastName.Should().Be(expectedReviewer.LastName);
    assignment.Submissions[0].Reviewers[0].FirstName.Should().Be(expectedReviewer.FirstName);
    await collaboratorClient.DidNotReceive().GetAll(repository.Id);
  }

  [Fact]
  public async Task AssignmentForRemoval_DoesNotRequireReviewerToBeInStudentList()
  {
    var repository = CreateRepository(100, "classroom-ue01-gh-mayr");
    assignmentClient.GetBySlug("swo3", "classroom", "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "Uebung 1", Slug = "ue01" }));
    searchClient.SearchRepo(Arg.Any<SearchRepositoriesRequest>()).Returns(
      Task.FromResult(new SearchRepositoryResult(1, false, [repository])));
    reviewerAssignmentsClient.Get("swo3", "classroom", "ue01").Returns(Task.FromResult(new ReviewerAssignmentsFile
    {
      AssignmentSlug = "ue01",
      Repositories = new(StringComparer.OrdinalIgnoreCase) { [repository.FullName] = ["unknown-reviewer"] }
    }));

    var parameters = new AssigmentParameters(10, "ue01", ClassroomName: "classroom", Org: "swo3", LoadReviewers: false);
    var assignment = await Assignment.FromGitHub(client, students, parameters);

    assignment.Submissions.Should().ContainSingle().Which.Reviewers.Should().BeEmpty();
  }

  [Fact]
  public async Task RemoveReviewers_WhenAssignmentsFileIsMissing_DeletesTeamsForDiscoveredRepositories()
  {
    var linked = CreateRepository(100, "classroom-ue01-gh-mayr");
    var unlinked = CreateRepository(101, "classroom-ue01-unknown");
    assignmentClient.GetBySlug("swo3", "classroom", "ue01").Returns(
      Task.FromResult(new AssignmentDto { Id = 10, Title = "Uebung 1", Slug = "ue01" }));
    searchClient.SearchRepo(Arg.Any<SearchRepositoriesRequest>()).Returns(
      Task.FromResult(new SearchRepositoryResult(2, false, [linked, unlinked])));
    var contents = Substitute.For<IRepositoryContentsClient>();
    contents.GetAllContents("swo3", "classroom50", "classroom/reviewers/ue01.json")
      .Returns<Task<IReadOnlyList<RepositoryContent>>>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));
    classroomsClient.ReviewerAssignments.Returns(new ReviewerAssignmentsClient(contents));
    var permissions = new TeamRepositoryPermissions(false, false, false, false, false);
    var linkedTeam = new Team("", "", 1, "", "linked", "classroom-ue01-gh-mayr-reviewers", "", TeamPrivacy.Closed, "", permissions, 0, 0, new Organization(), null, "");
    var unlinkedTeam = new Team("", "", 2, "", "unlinked", "classroom-ue01-unknown-reviewers", "", TeamPrivacy.Closed, "", permissions, 0, 0, new Organization(), null, "");
    teamsClient.GetAll("swo3").Returns(Task.FromResult<IReadOnlyList<Team>>([linkedTeam, unlinkedTeam]));
    var parameters = new AssigmentParameters(10, "ue01", ClassroomName: "classroom", Org: "swo3", LoadReviewers: false);

    var assignment = await Assignment.FromGitHub(client, students, parameters);
    await assignment.RemoveReviewers();

    assignment.Submissions.Should().ContainSingle();
    assignment.UnlinkedSubmissions.Should().ContainSingle();
    await teamsClient.Received(1).Delete(1);
    await teamsClient.Received(1).Delete(2);
    await contents.DidNotReceiveWithAnyArgs().CreateFile(default!, default!, default!, default!);
    await contents.DidNotReceiveWithAnyArgs().UpdateFile(default!, default!, default!, default!);
  }

  [Fact]
  public async Task SubmissionWithoutFeedbackPullRequest_DoesNotStopStatisticsLoading()
  {
    var pullRequestsClient = Substitute.For<IPullRequestsClient>();
    repositoriesClient.PullRequest.Returns(pullRequestsClient);
    var pullRequestReviewsClient = Substitute.For<IPullRequestReviewsClient>();
    pullRequestsClient.Review.Returns(pullRequestReviewsClient);
    pullRequestReviewsClient.GetAll(100, 1)
                            .Returns<Task<IReadOnlyList<PullRequestReview>>>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));

    var owner = students.LinkedStudents.Single(s => s.GitHubUsername == "gh-mayr");
    var submission = new Submission(client, CreateRepository(100, "repo1"), owner, []);
    var reviewStats = new Dictionary<(string Owner, string Reviewer), ReviewStatisticsItem>();
    var progress = Substitute.For<IProgress>();

    await submission.AddReviewStatistics(reviewStats, progress);

    reviewStats.Should().BeEmpty();
    progress.Received(1).WriteLine(Arg.Is<string>(message => message.Contains($"Repository \"swo3/repo1\" for \"{owner.FullName}\" has no feedback pull request.")), Console.Error);
  }

  private static Repository CreateRepository(int id, string repoName)
  {
    return new Repository("", htmlUrl: $"https://{repoName}", "", "", "", "", "", "", id, "", owner: null, repoName, $"swo3/{repoName}", false, "", "", "", true, false, 0, 0, "", 0, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, permissions: null, null, null, null, false, false, false, false, false, 0, 0, false, false, false, false, 0, false, RepositoryVisibility.Private, [], null, null, null, new());
  }
}