using Octokit;
using System.Text.RegularExpressions;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Domain;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public record AssigmentParameters(long ClassroomId, string AssignmentSlug, int? Group = null, string? ClassroomName = null, string? Org = null, bool LoadAssessments = false, bool LoadReviewers = true);

public class Assignment(IGitHubClassroomClient client,
                        string name,
                        DateTimeOffset? deadline,
                        IReadOnlyList<Submission> submissions,
                        IReadOnlyList<UnlinkedSubmission> unlinkedSubmissions,
                        string? metadataOrg = null,
                        string? metadataClassroomName = null,
                        ReviewerAssignmentsFile? reviewerAssignments = null)
{
  private readonly IGitHubClassroomClient client = client ?? throw new ArgumentNullException(nameof(client));
  private ReviewerAssignmentsFile reviewerAssignmentsDocument = reviewerAssignments ?? new();
  private readonly string? metadataOrg = metadataOrg;
  private readonly string? metadataClassroomName = metadataClassroomName;

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
    var reviewerAssignments = !string.IsNullOrWhiteSpace(parameters.Org) &&
                              !string.IsNullOrWhiteSpace(parameters.ClassroomName) &&
                              !string.IsNullOrWhiteSpace(assignmentDto.Slug)
      ? await client.Classroom.ReviewerAssignments.Get(parameters.Org, parameters.ClassroomName, assignmentDto.Slug)
      : new ReviewerAssignmentsFile { AssignmentSlug = assignmentDto.Slug };

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
          var reviewers = parameters.LoadReviewers ? LoadReviewers(owner, students, repository, reviewerAssignments) : [];

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


          var reviewers = parameters.LoadReviewers ? LoadReviewers(owner, students, repository, reviewerAssignments) : [];

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

    var assignment = new Assignment(client,
                                    assignmentDto.Title,
                                    assignmentDto.Deadline,
                                    submissions,
                                    unlinkedSubmission,
                                    parameters.Org,
                                    parameters.ClassroomName,
                                    reviewerAssignments)
    {
      Slug = assignmentDto.Slug
    };

    return assignment;
  }

  public IReadOnlyList<(Submission, Student)> FindReviewers()
  {
    return FindReviewers(Submissions);
  }

  public IReadOnlyList<(Submission, Student)> FindReviewersForValidUnassignedSubmissions()
  {
    var submissions = Submissions.Where(s => s.Assessment.IsValid() && s.Reviewers.Count == 0);
    return FindReviewers(submissions);
  }

  private IReadOnlyList<(Submission, Student)> FindReviewers(IEnumerable<Submission> submissionsToAssign)
  {
    try
    {
      var submissions = submissionsToAssign.ToList();
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
      try
      {
        await client.Issue.Comment.Create(org,
                                          submission.RepositoryName,
                                          Constants.FEEDBACK_PULLREQUEST_ID,
                                          notificationComment);
      }
      catch (NotFoundException)
      {
        var warning = $"Warning: Repository \"{submission.RepositoryFullName}\" for \"{submission.Owner.FullName}\" has no feedback pull request.";
        progress.WriteLineAndRedrawProgress(warning, Console.Error);
      }

      submission.Reviewers.Add(new Reviewer(reviewer));
      progress?.Increment();
    }

    await SaveReviewerAssignments();
  }

  public async Task RemoveReviewers(IProgress? progress = null)
  {
    var repositories = Submissions.Select(s => s.RepositoryFullName)
      .Concat(UnlinkedSubmissions.Select(s => s.RepositoryFullName))
      .Concat(reviewerAssignmentsDocument.Repositories.Keys)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    var repositoriesByOrg = repositories.Select(fullName =>
    {
      var parts = fullName.Split('/');
      if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace) ||
          (!string.IsNullOrWhiteSpace(metadataOrg) && !string.Equals(parts[0], metadataOrg, StringComparison.OrdinalIgnoreCase)))
      {
        throw new SubmissionException($"Invalid repository full name \"{fullName}\" in reviewer assignments.");
      }
      return (Org: parts[0], Name: parts[1]);
    }).GroupBy(repository => repository.Org, StringComparer.OrdinalIgnoreCase).ToList();

    progress?.Init(repositories.Count);
    foreach (var group in repositoriesByOrg)
    {
      var teams = await client.Organization.Team.GetAll(group.Key);
      foreach (var repository in group)
      {
        var teamName = GetReviewerTeamName(repository.Name);
        var reviewerTeam = teams.SingleOrDefault(t => string.Equals(t.Name, teamName, StringComparison.OrdinalIgnoreCase));
        if (reviewerTeam is not null)
        {
          try
          {
            await client.Organization.Team.Delete(reviewerTeam.Id);
          }
          catch (NotFoundException)
          {
            if (await FindReviewerTeam(group.Key, teamName) is not null)
            {
              throw;
            }
          }
        }

        progress?.Increment();
      }
    }

    if (reviewerAssignmentsDocument.Repositories.Count > 0 &&
        !string.IsNullOrWhiteSpace(metadataOrg) &&
        !string.IsNullOrWhiteSpace(metadataClassroomName) &&
        !string.IsNullOrWhiteSpace(Slug))
    {
      var emptyAssignments = new ReviewerAssignmentsFile { AssignmentSlug = Slug };
      await client.Classroom.ReviewerAssignments.Save(metadataOrg, metadataClassroomName, emptyAssignments);
      reviewerAssignmentsDocument = emptyAssignments;
    }

    foreach (var submission in Submissions)
    {
      submission.Reviewers.Clear();
    }
  }

  private async Task SaveReviewerAssignments()
  {
    if (string.IsNullOrWhiteSpace(metadataOrg) ||
        string.IsNullOrWhiteSpace(metadataClassroomName) ||
        string.IsNullOrWhiteSpace(Slug))
    {
      return;
    }

    var repositories = new Dictionary<string, List<string>>(reviewerAssignmentsDocument.Repositories, StringComparer.OrdinalIgnoreCase);
    foreach (var submission in Submissions)
    {
      repositories[submission.RepositoryFullName] = submission.Reviewers
        .Select(reviewer => reviewer.GitHubUsername)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    var updatedReviewerAssignments = new ReviewerAssignmentsFile
    {
      AssignmentSlug = Slug,
      Repositories = repositories
    };

    await client.Classroom.ReviewerAssignments.Save(metadataOrg, metadataClassroomName, updatedReviewerAssignments);
    reviewerAssignmentsDocument = updatedReviewerAssignments;
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
      await submission.AddReviewStatistics(reviewStats, progress);
      progress?.Increment();
    }

    return reviewStats;
  }

  private static List<Reviewer> LoadReviewers(Student owner, IStudentList students, Repository repository, ReviewerAssignmentsFile reviewerAssignments)
  {
    var reviewers = new List<Reviewer>();
    var reviewerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (reviewerAssignments.Repositories.TryGetValue(repository.FullName, out var reviewerLogins))
    {
      foreach (var reviewerLogin in reviewerLogins.Where(login => !string.IsNullOrWhiteSpace(login) &&
                                                                   !string.Equals(login, owner.GitHubUsername, StringComparison.OrdinalIgnoreCase)))
      {
        AddReviewer(students, reviewers, reviewerNames, reviewerLogin);
      }
    }

    return reviewers;
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

    var authenticatedUser = await client.User.Current();
    Team createdTeam;

    try
    {
      createdTeam = await client.Organization.Team.Create(org, newTeam);
    }
    // Another caller may create the team after the initial lookup; recheck before rethrowing.
    catch (ApiValidationException)
    {
      var concurrentTeam = await FindReviewerTeam(org, teamName);
      if (concurrentTeam is not null)
      {
        return concurrentTeam;
      }

      throw;
    }

    await client.Organization.Team.RemoveMembership(createdTeam.Id, authenticatedUser.Login);
    return createdTeam;
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

  private static void AddReviewer(IStudentList students, IList<Reviewer> reviewers, ISet<string> reviewerNames, string reviewerLogin)
  {
    if (!students.TryGetValue(reviewerLogin, out var reviewer))
    {
      throw new SubmissionException($"No student assigned to reviewer \"{reviewerLogin}\".");
    }

    if (reviewerNames.Add(reviewer.GitHubUsername))
    {
      reviewers.Add(new Reviewer(reviewer));
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