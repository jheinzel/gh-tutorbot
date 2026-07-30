using Octokit;
using System.Text.RegularExpressions;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Domain;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public record AssigmentParameters(long ClassroomId, string AssignmentSlug, int? Group = null, string? ClassroomName = null, string? Org = null, bool LoadAssessments = false);

public class Assignment(IGitHubClassroomClient client, string name, DateTimeOffset? deadline, IReadOnlyList<Submission> submissions, IReadOnlyList<UnlinkedSubmission> unlinkedSubmissions)
{
  private readonly IGitHubClassroomClient client = client ?? throw new ArgumentNullException(nameof(client));

  public string Name { get; init; } = name ?? throw new ArgumentNullException(nameof(name));
  public DateTimeOffset? Deadline { get; init; } = deadline;
  public string Slug { get; init; } = string.Empty;

  public IReadOnlyList<Submission> Submissions { get; init; } = submissions ?? throw new ArgumentNullException(nameof(submissions));

  public IReadOnlyList<UnlinkedSubmission> UnlinkedSubmissions { get; init; } = unlinkedSubmissions ?? throw new ArgumentNullException(nameof(unlinkedSubmissions));

  public static async Task<Assignment> FromGitHub(IGitHubClassroomClient client, IStudentList students, AssigmentParameters parameters, ConfigurationHelper? configuration = null, IProgress? progress = null)
  {
    var submissions = new List<Submission>();
    var unlinkedSubmission = new List<UnlinkedSubmission>();

    var assignmentDto = !string.IsNullOrWhiteSpace(parameters.Org) && !string.IsNullOrWhiteSpace(parameters.ClassroomName)
      ? await client.Classroom.Assignment.GetBySlug(parameters.Org, parameters.ClassroomName, parameters.AssignmentSlug)
      : await client.Classroom.Assignment.GetBySlug(parameters.ClassroomId, parameters.AssignmentSlug);
    if (!string.IsNullOrWhiteSpace(parameters.Org) && !string.IsNullOrWhiteSpace(parameters.ClassroomName) && !string.IsNullOrWhiteSpace(assignmentDto.Slug))
    {
      var assignmentPrefix = $"{parameters.ClassroomName}-{assignmentDto.Slug}-";

      var repoCandidates = new List<Repository>();
      var searchRequest = new SearchRepositoriesRequest($"{assignmentPrefix} in:name org:{parameters.Org}")
      {
        PerPage = 100,
        Page = 1
      };

      while (true)
      {
        var searchResult = await client.Search.SearchRepo(searchRequest);
        repoCandidates.AddRange(searchResult.Items);

        if (searchResult.Items.Count < searchRequest.PerPage)
        {
          break;
        }

        searchRequest.Page++;
      }

      var matchingRepos = repoCandidates
        .GroupBy(r => r.Id)
        .Select(g => g.First())
        .Where(r => r.Name.StartsWith(assignmentPrefix, StringComparison.OrdinalIgnoreCase)
                 && (string.IsNullOrWhiteSpace(assignmentDto.TemplateRepoName) || !r.Name.Equals(assignmentDto.TemplateRepoName, StringComparison.OrdinalIgnoreCase)))
        .ToList();

      progress?.Init(matchingRepos.Count);

      foreach (var repository in matchingRepos)
      {
        var ownerLogin = repository.Name[assignmentPrefix.Length..];
        if (!students.TryGetValue(ownerLogin, out var owner))
        {
          unlinkedSubmission.Add(new UnlinkedSubmission(repository));
          progress?.Increment();
          continue;
        }

        if (parameters.Group is null || owner.GroupNr == parameters.Group)
        {
          List<Reviewer> reviewers = await LoadReviewers(client, owner, students, repository);

          var submission = new Submission(client, repository, owner, reviewers);
          if (parameters.LoadAssessments)
          {
            await submission.Assessment.Load(client, repository.Id);
          }
          submissions.Add(submission);
        }

        progress?.Increment();
      }
    }
    else
    {
      progress?.Init(assignmentDto.Accepted * 2);
      var submissionDtos = await client.Classroom.Submissions.GetAll(assignmentDto.Id, progress);

      foreach (var submissionDto in submissionDtos)
      {
        if (submissionDto.Repository is null)
        {
          throw new SubmissionException($"No repository assigned to submission with ID \"{submissionDto.Id}\".");
        }

        if (submissionDto.Students.Count == 0)
        {
          throw new SubmissionException($"No owner assigned assigned to repository \"{submissionDto.Id}\".");
        }
        if (submissionDto.Students.Count > 1)
        {
          throw new SubmissionException($"More than one owner assigned to repository \"{submissionDto?.Repository.FullName}\".");
        }

        if (!students.TryGetValue(submissionDto.Students[0].Login, out var owner))
        {
          var repo = await client.Repository.Get(submissionDto.Repository.Id);
          unlinkedSubmission.Add(new UnlinkedSubmission(repo));
          progress?.Increment();
          continue;
        }

        if (parameters.Group is null || owner.GroupNr == parameters.Group)
        {
          var repository = await client.Repository.Get(submissionDto.Repository.Id);


          List<Reviewer> reviewers = await LoadReviewers(client, owner, students, repository);

          var submission = new Submission(client, repository, owner, reviewers);
          if (parameters.LoadAssessments)
          {
            await submission.Assessment.Load(client, repository.Id);
          }
          submissions.Add(submission);
        }

        progress?.Increment();
      }
    }

    var assignment = new Assignment(client, assignmentDto.Title, assignmentDto.Deadline, submissions, unlinkedSubmission)
    {
      Slug = assignmentDto.Slug
    };

    return assignment;
  }

  public IReadOnlyList<(Submission, Student)> FindReviewers()
  {
    try
    {
      var submissions = Submissions.ToList();
      submissions.Shuffle();

      var studentToSubmission = new Dictionary<Student, Submission>();
      submissions.ForEach(s => studentToSubmission.Add(s.Owner, s));

      var existingAssignments = submissions.Where(s => s.Reviewers.Count > 0).Select(s => (s, studentToSubmission[s.Reviewers[0]]));

      var mapping = new EntityMapper<Submission>(submissions, existingAssignments);

      return mapping.FindUniqueMapping()
                    .Select(pair => (pair.Key, pair.Value.Owner))
                    .ToList().AsReadOnly();
    } 
    catch (NonUniqueValuesException<Submission> ex)
    {
      throw new ReviewerAssignmentException($"Reviewers are assigned to multiple submissions: {ex.NonUniqueValues.ToStringWithSeparator()}");
    }
  }

  public async Task AssignReviewers(IEnumerable<(Submission, Student)> reviewers, IProgress? progress = null)
  {
    progress?.Init(reviewers.Count());

    foreach (var (submission, reviewer) in reviewers)
    {
      var org = GetRepositoryOrganization(submission.RepositoryFullName);
      var teamName = GetReviewerTeamName(submission.RepositoryName);
      var reviewerTeam = await GetOrCreateReviewerTeam(org, teamName);

      await client.Organization.Team.AddOrEditMembership(reviewerTeam.Id,
                                                         reviewer.GitHubUsername,
                                                         new UpdateTeamMembership(TeamRole.Member));

      await client.Organization.Team.AddOrUpdateTeamRepositoryPermissions(org,
                                                                          reviewerTeam.Slug,
                                                                          org,
                                                                          submission.RepositoryName,
                                                                          Constants.GITHUB_TEAM_READ_PERMISSION);

      var pullRequestUrl = $"https://github.com/{org}/{submission.RepositoryName}/pull/{Constants.FEEDBACK_PULLREQUEST_ID}";
      var notificationComment = string.Format(Constants.REVIEWER_NOTIFICATION_COMMENT,
                                              reviewer.GitHubUsername,
                                              submission.RepositoryUrl,
                                              pullRequestUrl);
      await client.Issue.Comment.Create(org,
                                        submission.RepositoryName,
                                        Constants.FEEDBACK_PULLREQUEST_ID,
                                        notificationComment);

      submission.Reviewers.Add(new Reviewer(reviewer));
      progress?.Increment();
    }
  }

  public async Task RemoveReviewers(IProgress? progress = null)
  {
    progress?.Init(Submissions.Count);

    foreach (var submission in Submissions)
    {
      var org = GetRepositoryOrganization(submission.RepositoryFullName);
      var teamName = GetReviewerTeamName(submission.RepositoryName);
      var reviewerTeam = await FindReviewerTeam(org, teamName);
      if (reviewerTeam is not null)
      {
        await client.Organization.Team.Delete(reviewerTeam.Id);
      }

      submission.Reviewers.Clear();
      progress?.Increment();
    }
  }

  public async Task<ReviewStatistics> GetReviewStatistics(IProgress? progress = null)
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
      await submission.AddReviewStatistics(reviewStats);
      progress?.Increment();
    }

    return reviewStats;
  }

  private static async Task<List<Reviewer>> LoadReviewers(IGitHubClassroomClient client, Student owner, IStudentList students, Repository repository)
  {
    var reviewers = new List<Reviewer>();
    var reviewerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var org = GetRepositoryOrganization(repository.FullName);
    var teamName = GetReviewerTeamName(repository.Name);
    var reviewerTeam = await FindReviewerTeam(client, org, teamName);
    if (reviewerTeam is not null)
    {
      var members = await client.Organization.Team.GetAllMembers(reviewerTeam.Id);
      foreach (var member in members.Where(m => !string.IsNullOrWhiteSpace(m.Login) && m.Login != owner.GitHubUsername))
      {
        if (students.TryGetValue(member.Login, out var reviewer) && reviewerNames.Add(reviewer.GitHubUsername))
        {
          reviewers.Add(new Reviewer(reviewer));
        }
      }
    }

    // Backward compatibility for existing direct collaborator/invitation based reviewer assignments.
    var readOnlyCollaborators = (await client.Repository.Collaborator.GetAll(repository.Id))
                                  .Where(c => IsReadPermission(c.RoleName))
                                  .ToList();

    foreach (var collaborator in readOnlyCollaborators.Where(c => !string.IsNullOrWhiteSpace(c.Login) && c.Login != owner.GitHubUsername))
    {
      AddReviewer(students, reviewers, reviewerNames, collaborator.Login);
    }

    var invitations = (await client.Repository.Invitation.GetAllForRepository(repository.Id)).ToList();
    foreach (var invitation in invitations.Where(i => IsReadPermission(i.Permissions.StringValue) &&
                                                      i.Invitee is not null &&
                                                      !string.IsNullOrWhiteSpace(i.Invitee.Login)))
    {
      AddReviewer(students, reviewers, reviewerNames, invitation.Invitee.Login, invitation.Id);
    }

    return reviewers;
  }

  private static bool IsReadPermission(string? permission)
  {
    return string.Equals(permission, Constants.GITHUB_READ_ROLE, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(permission, Constants.GITHUB_TEAM_READ_PERMISSION, StringComparison.OrdinalIgnoreCase);
  }

  private async Task<Team> GetOrCreateReviewerTeam(string org, string teamName)
  {
    var existingTeam = await FindReviewerTeam(org, teamName);
    if (existingTeam is not null)
    {
      return existingTeam;
    }

    var newTeam = new NewTeam(teamName)
    {
      Privacy = TeamPrivacy.Closed
    };

    try
    {
      return await client.Organization.Team.Create(org, newTeam);
    }
    catch (ApiValidationException)
    {
      var concurrentTeam = await FindReviewerTeam(org, teamName);
      if (concurrentTeam is not null)
      {
        return concurrentTeam;
      }

      throw;
    }
  }

  private async Task<Team?> FindReviewerTeam(string org, string teamName)
  {
    return await FindReviewerTeam(client, org, teamName);
  }

  private static async Task<Team?> FindReviewerTeam(IGitHubClassroomClient client, string org, string teamName)
  {
    var teams = await client.Organization.Team.GetAll(org);
    return teams.SingleOrDefault(t => string.Equals(t.Name, teamName, StringComparison.OrdinalIgnoreCase));
  }

  private static void AddReviewer(IStudentList students, IList<Reviewer> reviewers, ISet<string> reviewerNames, string reviewerLogin, long? invitationId = null)
  {
    if (!students.TryGetValue(reviewerLogin, out var reviewer))
    {
      throw new SubmissionException($"No student assigned to reviewer \"{reviewerLogin}\".");
    }

    if (reviewerNames.Add(reviewer.GitHubUsername))
    {
      reviewers.Add(new Reviewer(reviewer, invitationId));
    }
  }

  private static string GetReviewerTeamName(string repositoryName)
  {
    return $"{repositoryName}-reviewers";
  }

  private static string GetRepositoryOrganization(string repositoryFullName)
  {
    var parts = repositoryFullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2)
    {
      throw new SubmissionException($"Invalid repository full name \"{repositoryFullName}\".");
    }

    return parts[0];
  }
}