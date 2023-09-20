using System.ComponentModel;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.ListExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic
  ;

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

  public static async Task<Assignment> FromGitHub(IGitHubClient client, StudentList students, long classroomId, string assignmentName)
  {
    var submissions = new List<Submission>();

    var assignmentDto = await client.Classroom().Assignment.GetByName(classroomId, assignmentName);
    var submissionDtos = await client.Classroom().Submissions.GetAll(assignmentDto.Id);


    //var repositoryIds = response.Body.Where(s => s.Repository is not null)
    //                                 .Select(s => s.Repository!.Id).ToList();


    //await Task.WhenAll(repositoryIds.Select(id => this.Repository.Get(id)));

    foreach (var submissionDto in submissionDtos)
    {
      //var collaborators = (await client.Repository.Collaborator.GetAll(repo.Id))
      //                      .Where(c => c.Permissions.Maintain is not null && c.Permissions.Maintain == false)
      //                      .ToList();
      // var collaboratorsWithWriteAccess = collaborators.Where(c => c.RoleName == Constants.GITHUB_WRITE_ROLE).ToList();

      if (submissionDto.Repository is null)
      {
        throw new SubmissionException($"No repository assign to submission with ID \"{submissionDto.Id}\"");
      }

      var repo = await client.Repository.Get(submissionDto.Repository.Id);

      if (submissionDto.Students.Count == 0)
      {
        throw new SubmissionException($"No owner assigned assigned to repository \"{submissionDto.Id}\".");
      }
      if (submissionDto.Students.Count > 1)
      {
        throw new SubmissionException($"More than one owner assigned to repository \"{repo.Name}\".");
      }
      if (!students.TryGetValue(submissionDto.Students[0].Login, out var owner))
      {
        throw new SubmissionException($"No student assigned to GitHub user \"{submissionDto.Students[0].Login}\".");
      }

      var reviewers = new List<Reviewer>();

      var readOnlyCollaborators = (await client.Repository.Collaborator.GetAll(repo.Id))
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

      var invitations = (await client.Repository.Invitation.GetAllForRepository(repo.Id)).ToList();
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

      submissions.Add(new Submission
      {
        RepositoryId = repo.Id,
        RepositoryName = repo.Name,
        RepositoryFullName = repo.FullName,
        RepositoryUrl = repo.HtmlUrl,
        Owner = owner,
        Reviewers = reviewers
      });
    }

    return new Assignment(client, assignmentDto.Title, assignmentDto.Deadline?.ToDateTime(), submissions);
  }

  public async Task AssignReviewers()
  {
    await RemoveReviewers();

    if (Submissions.Count() == 0)
    {
      return;
    }

    var submissions = Submissions.ToList(); // create a shallow copy of the submissions list
    submissions.Shuffle();
    for (int i = 0; i < submissions.Count; i++)
    {
      int j = (i + 1) % submissions.Count;
      var invitation = await client.Repository.Collaborator.Add(submissions[i].RepositoryId, submissions[j].Owner.GitHubUsername, new CollaboratorRequest(Constants.GITHUB_READ_ROLE));
      submissions[i].Reviewers.Add(new Reviewer(submissions[j].Owner, invitation.Id));
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


  public async Task<ReviewStatistics> GetReviewStatistics(IGitHubClient client, StudentList students)
  {
    var reviewStats = new Dictionary<(string Owner, string Reviewer), Logic.ReviewStatisticsItem>();
    
    foreach (var submission in Submissions)
    {
      await submission.AddReviewStatistics(client, students, reviewStats);
    }

    return reviewStats;
  }
}