using Octokit;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;

namespace TutorBot.Domain;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public record AssigmentParameters(long ClassroomId, string AssignmentName, bool LoadAssessments = false);

public class Assignment
{
  private IGitHubClassroomClient client;

  public string Name { get; init; }
  public DateTimeOffset? Deadline { get; init; }

  public IReadOnlyList<Submission> Submissions { get; init; }

  public IReadOnlyList<UnlinkedSubmission> UnlinkedSubmissions { get; init; }

  public Assignment(IGitHubClassroomClient client, string name, DateTimeOffset? deadline, IReadOnlyList<Submission> submissions, IReadOnlyList<UnlinkedSubmission> unlinkedSubmissions)
  {
    this.client = client ?? throw new ArgumentNullException(nameof(client));
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Deadline = deadline;
    Submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
    UnlinkedSubmissions = unlinkedSubmissions ?? throw new ArgumentNullException(nameof(unlinkedSubmissions));
  }

  public static async Task<Assignment> FromGitHub(IGitHubClassroomClient client, IStudentList students, AssigmentParameters parameters, IProgress? progress = null)
  {
    var submissions = new List<Submission>();
    var unlinkedSubmission = new List<UnlinkedSubmission>();

    var assignmentDto = await client.Classroom.Assignment.GetByName(parameters.ClassroomId, parameters.AssignmentName);
    progress?.Init(assignmentDto.Accepted * 2);
    var submissionDtos = await client.Classroom.Submissions.GetAll(assignmentDto.Id, progress);

    foreach (var submissionDto in submissionDtos)
    {
      if (submissionDto.Repository is null)
      {
        throw new SubmissionException($"No repository assigned to submission with ID \"{submissionDto.Id}\".");
      }

      var repository = await client.Repository.Get(submissionDto.Repository.Id);

      if (submissionDto.Students.Count == 0)
      {
        throw new SubmissionException($"No owner assigned assigned to repository \"{submissionDto.Id}\".");
      }
      if (submissionDto.Students.Count > 1)
      {
        throw new SubmissionException($"More than one owner assigned to repository \"{repository.Name}\".");
      }
      if (!students.TryGetValue(submissionDto.Students[0].Login, out var owner))
      {
        unlinkedSubmission.Add(new UnlinkedSubmission(repository));
        progress?.Increment();
        continue;
      }

      var reviewers = new List<Reviewer>();

      var readOnlyCollaborators = (await client.Repository.Collaborator.GetAll(repository.Id))
                                    .Where(c => c.Permissions.Maintain is not null &&
                                                c.Permissions.Maintain == false &&
                                                c.RoleName == Constants.GITHUB_READ_ROLE)
                                    .ToList();

      foreach (var collaborator in readOnlyCollaborators)
      {
        if (students.TryGetValue(collaborator.Login, out var reviewer))
        {
          reviewers.Add(new Reviewer(reviewer));
        }
        else
        {
          throw new SubmissionException($"No student assigned to reviewer \"{collaborator.Login}\".");
        }
      }

      var invitations = (await client.Repository.Invitation.GetAllForRepository(repository.Id)).ToList();
      foreach (var invitation in invitations.Where(i => i.Permissions == Constants.GITHUB_READ_ROLE))
      {
        if (students.TryGetValue(invitation.Invitee.Login, out var reviewer))
        {
          reviewers.Add(new Reviewer(reviewer, invitation.Id));
        }
        else
        {
          throw new SubmissionException($"No student assigned to reviewer \"{invitation.Invitee.Login}\".");
        }
      }

      var submission = new Submission(client, repository, owner, reviewers);
      if (parameters.LoadAssessments)
      {
        await submission.Assessment.Load(client, repository.Id);
      }
      submissions.Add(submission);

      progress?.Increment();
    }

    return new Assignment(client, assignmentDto.Title, assignmentDto.Deadline, submissions, unlinkedSubmission);
  }


  public IReadOnlyList<(Submission, Student)> FindReviewers()
  {
    var validSubmissions = Submissions.Where(s => s.Assessment.IsValid()).ToList();
    validSubmissions.Shuffle();

    var studentToSubmission = new Dictionary<Student, Submission>();
    validSubmissions.ForEach(s => studentToSubmission.Add(s.Owner, s));

    var existingAssignments = validSubmissions.Where(s => s.Reviewers.Count > 0).Select(s => (s, studentToSubmission[s.Reviewers[0]]));
    
    var mapping = new EntityMapper<Submission>(validSubmissions, existingAssignments);

    return mapping.FindUniqueMapping()
                  .Select(pair => (pair.Key, pair.Value.Owner))
                  .ToList().AsReadOnly();
  }

  public async Task AssignReviewers(IEnumerable<(Submission, Student)> reviewers, IProgress? progress = null)
  {
    progress?.Init(reviewers.Count());
    var readRequest = new CollaboratorRequest(Constants.GITHUB_READ_ROLE);

    foreach (var (submission, reviewer) in reviewers)
    {
      RepositoryInvitation? invitation = await client.Repository.Collaborator.Add(submission.RepositoryId, reviewer.GitHubUsername, readRequest);
      submission.Reviewers.Add(new Reviewer(reviewer, invitation?.Id));
      progress?.Increment();
    }
  }

  public async Task RemoveReviewers(IProgress? progress = null)
  {
    progress?.Init(Submissions.Count );

    foreach (var submission in Submissions)
    {
      foreach (var reviewer in submission.Reviewers)
      {
        if (reviewer.IsInvitationPending)
        {
          await client.Repository.Invitation.Delete(submission.RepositoryId, reviewer.InvitationId!.Value);
        }
        else
        {
          await client.Repository.Collaborator.Delete(submission.RepositoryId, reviewer.GitHubUsername);
        }

        progress?.Increment();
      }

      submission.Reviewers.Clear();
    }
  }

  public async Task<ReviewStatistics> GetReviewStatistics(IStudentList students, IProgress? progress = null)
  {
    var reviewStats = new Dictionary<(string Owner, string Reviewer), Domain.ReviewStatisticsItem>();
    foreach (var submission in Submissions)
    {
      foreach (var reviewer in submission.Reviewers)
      {
        reviewStats.Add((submission.Owner.GitHubUsername, reviewer.GitHubUsername), new ReviewStatisticsItem());
      }
    }

    progress?.Init(Submissions.Count);

    foreach (var submission in Submissions)
    {
      await submission.AddReviewStatistics(students, reviewStats);
      progress?.Increment();
    }

    return reviewStats;
  }
}