using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Octokit;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Tests;

public class AssignReviewersTests
{
  private readonly IGitHubClassroomClient client;
  private readonly IRepositoriesClient repositoriesClient;
  private readonly IRepositoryContentsClient repositoryContentsClient;
  private readonly IRepoCollaboratorsClient repoCollaboratorsClient;

  private Repository repository;

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
    var readRequest = new CollaboratorRequest("read");
    var invitation = new RepositoryInvitation(1, "", repository, null, null, InvitationPermissionType.Read, DateTimeOffset.Now, false, "", "");
    repoCollaboratorsClient.Add(Arg.Any<long>(), Arg.Any<string>(), Arg.Is<CollaboratorRequest>(r => r.Permission == "read")).Returns(Task.FromResult(invitation));

    repositoryContentsClient = Substitute.For<IRepositoryContentsClient>();
    client.Repository.Content.Returns(repositoryContentsClient);

    var encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(assessmentString));
    RepositoryContent reopContent = new RepositoryContent("", "", "", 0, ContentType.File, "", "", "", "", "", encodedContent, "", "");
    repositoryContentsClient.GetAllContents(Arg.Any<long>(), Arg.Any<string>()).Returns(Task.FromResult<IReadOnlyList<RepositoryContent>>(new List<RepositoryContent> { reopContent }));

    repository = CreateRepository(100, "repo1");
  }

  private Repository CreateRepository(int id, string repoName)
  {
    return new Repository("", htmlUrl: $"https://{repoName}", "", "", "", "", "", id, "", owner: null, repoName, $"swo3/{repoName}", false, "", "", "", true, false, 0, 0, "", 0, DateTimeOffset.Now, DateTimeOffset.Now, DateTimeOffset.Now, permissions: null, null, null, null, false, false, false, false, 0, 0, false, false, false, false, 0, false, RepositoryVisibility.Private, Enumerable.Empty<string>(), null, null);
  }

  private IList<Student> CreateStudentList(int n)
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
 
    var submissions = new List<Submission>
    {
      new Submission(client, repository, students[0], emptyReviewerList)
    };
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

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

    var submissions = new List<Submission>
    {
      new Submission(client, repository, students[0], emptyReviewerList),
      new Submission(client, repository, students[1], emptyReviewerList)
    };
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();

    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    IList<(Submission Submission, Student Reviewer)> reviewers = assignment.FindReviewers().ToList();
    reviewers.Should().HaveCount(2);
    reviewers[0].Submission.Owner.Should().Be(reviewers[1].Reviewer);
  }

  [Theory]
  [InlineData(3)]
  [InlineData(10)]
  [InlineData(30)]
  public async Task ReviewerAssignment_WithNoPreAssignedReviewers_ShouldBeCorrect(int numSubmission)
  {
    var students = CreateStudentList(numSubmission);
    var emptyReviewerList = new List<Reviewer>();

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

    var submissions = new List<Submission>
    {
      new Submission(client, repository, students[0], new List<Reviewer> { new Reviewer(students[1]) }),
      new Submission(client, repository, students[1], emptyReviewerList),
      new Submission(client, repository, students[2], emptyReviewerList)
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
  public async Task ReviewerAssignment_WithPreAssignedReviewers_ShouldBeCorrect(int numSubmission)
  {
    var students = CreateStudentList(numSubmission);
    var emptyReviewerList = new List<Reviewer>();

    var submissions = new List<Submission>();
    for (int i = 0; i < numSubmission; i++)
    {
      if (i % 2 == 0)
      {
        submissions.Add(new Submission(client, repository, students[i], emptyReviewerList));
      }
      else
      {
        submissions.Add(new Submission(client, repository, students[i], new List<Reviewer> { new Reviewer(students[i - 1]) }));
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

    var submissions = new List<Submission>();
    for (int i = 0; i < 3; i++)
    {
      submissions.Add(new Submission(client, repository, students[i], emptyReviewerList));
    }
    foreach (var s in submissions) await s.Assessment.Load(client, repository.Id);

    var unlinkedSubmissions = new List<UnlinkedSubmission>();
    var assignment = new Assignment(client, "ue01", DateTime.Now.AddDays(1), submissions, unlinkedSubmissions);

    var reviewers = new List<(Submission, Student)>
    {
      ( submissions[0], students[1] ),
      ( submissions[1], students[2] ),
      ( submissions[2], students[0] )
    };

    await assignment.AssignReviewers(reviewers);

    ReviewerAssignmentShouldBeCorrect(assignment);
  }
}