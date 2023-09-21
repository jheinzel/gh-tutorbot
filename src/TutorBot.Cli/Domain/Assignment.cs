using System.ComponentModel;
using System.Linq;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.ListExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public class Assignment
{
  private IGitHubClient client;

  public string Name { get; init; }
  public DateTime? Deadline { get; init; }

  public IEnumerable<Submission> Submissions { get; init; }

  private Assignment(IGitHubClient client, string name, DateTime? deadline, IEnumerable<Submission> submissions)
  {
    this.client = client ?? throw new ArgumentNullException(nameof(client));
    Name = name ?? throw new ArgumentNullException(nameof(name));
    Deadline = deadline;
    Submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
  }

  public static async Task<Assignment> FromGitHub(IGitHubClient client, StudentList students, long classroomId, string assignmentName, bool loadAssessments = false)
  {
    var submissions = new List<Submission>();

    var assignmentDto = await client.Classroom().Assignment.GetByName(classroomId, assignmentName);
    var submissionDtos = await client.Classroom().Submissions.GetAll(assignmentDto.Id);


    foreach (var submissionDto in submissionDtos)
    {
      if (submissionDto.Repository is null)
      {
        throw new SubmissionException($"No repository assign to submission with ID \"{submissionDto.Id}\"");
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
        throw new SubmissionException($"No student assigned to GitHub user \"{submissionDto.Students[0].Login}\".");
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
      if (loadAssessments)
      {
        await submission.Assessment.Load(client, repository.Id);
      }
      submissions.Add(submission);
    }

    return new Assignment(client, assignmentDto.Title, assignmentDto.Deadline?.ToDateTime(), submissions);
  }


  public async Task AssignReviewers()
  {
    await RemoveReviewers();

    // create a shallow copy of the submissions list
    // only consider submissions with a valid assessment
    // (i.e. assessment file exists, has the correct format
    // and the total grading is greater than 0)
    var validSubmissions = Submissions.Where(s => s.Assessment.IsValid()).ToList();
    if (validSubmissions.Count() <= 1)
    {
      return;
    }

    validSubmissions.Shuffle();

    var readRequest = new CollaboratorRequest(Constants.GITHUB_READ_ROLE);

    for (int i = 0; i < validSubmissions.Count; i++)
    {
      int j = (i + 1) % validSubmissions.Count;
      var owner = validSubmissions[i].Owner;
      var reviewer = validSubmissions[j].Owner;

      var invitation = await client.Repository.Collaborator.Add(validSubmissions[i].RepositoryId, reviewer.GitHubUsername, readRequest);
      if (invitation is null)
      {
        throw new LogicException($"Cannot assign reviewer \"{reviewer.GitHubUsername}\" to \"{owner.GitHubUsername}\"");
      }
      validSubmissions[i].Reviewers.Add(new Reviewer(reviewer, invitation.Id));
    }
  }

  public async Task RemoveReviewers()
  {
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
      }

      submission.Reviewers.Clear();
    }
  }

  public async Task CloneRepositories(string directory, Action<string> successAction, Action<string, string, int> failureAction)
  {
    foreach (var submission in Submissions)
    {
      try
      {
        var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync("gh", $"repo clone {submission.RepositoryFullName} {directory}/{submission.RepositoryName}");
        if (exitCode == 0)
        {
          successAction(submission.RepositoryFullName);
        }
        else
        {
          failureAction(submission.RepositoryFullName, errorResult ?? "", exitCode);
        }
      }
      catch (Win32Exception)
      {
        throw new LogicException("Error: Command \"gh\" (GitHub CLI) not found");
      }
    }
  }


  public async Task<ReviewStatistics> GetReviewStatistics(StudentList students)
  {
    var reviewStats = new Dictionary<(string Owner, string Reviewer), Logic.ReviewStatisticsItem>();
    
    foreach (var submission in Submissions)
    {
      await submission.AddReviewStatistics(students, reviewStats);
    }

    return reviewStats;
  }
}