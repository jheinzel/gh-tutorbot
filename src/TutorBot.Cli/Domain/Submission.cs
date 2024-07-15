using Octokit;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;

namespace TutorBot.Domain;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public class Submission(IGitHubClassroomClient client, Repository repository, Student owner, IEnumerable<Reviewer> reviewers)
{
  private readonly IGitHubClassroomClient client = client;
  private readonly Repository repository = repository;
  
  public long RepositoryId => repository.Id;
  public string RepositoryName => repository.Name;
  public string RepositoryFullName => repository.FullName;
  public string RepositoryUrl => repository.HtmlUrl;
  public Student Owner { get; init; } = owner;

  public IList<Reviewer> Reviewers { get; set; } = reviewers.ToList();

  public Assessment Assessment { get; private set; } = new Assessment();

  public async Task AddReviewStatistics(ReviewStatistics reviewStats)
  {
    var reviews = await client.Repository.PullRequest.Review.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    foreach (var review in reviews)
    {
      if (!reviewStats.TryGetValue((Owner.GitHubUsername, review.User.Login), out Domain.ReviewStatisticsItem? stats))
      {
        stats = new Domain.ReviewStatisticsItem();
        reviewStats.Add((Owner.GitHubUsername, review.User.Login), stats);
      }

      var reviewDate = review.SubmittedAt;
      if (stats.LastReviewDate is null || reviewDate > stats.LastReviewDate)
      {
        stats.LastReviewDate = reviewDate;
      }

      stats.NumReviews++;
      stats.NumWords += review.Body.WordCount();
    }

    var comments = await client.Repository.PullRequest.ReviewComment.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    foreach (var comment in comments)
    {
      if (!reviewStats.TryGetValue((Owner.GitHubUsername, comment.User.Login), out Domain.ReviewStatisticsItem? stats))
      {
        stats = new Domain.ReviewStatisticsItem();
        reviewStats.Add((Owner.GitHubUsername, comment.User.Login), stats);
      }

      stats.NumComments++;
      stats.NumWords += comment.Body.WordCount();
    }

    reviewStats.Remove((Owner.GitHubUsername, Owner.GitHubUsername));
  }
}

public class UnlinkedSubmission(Repository repository)
{
  public long RepositoryId => repository.Id;
  public string RepositoryName => repository.Name;
  public string RepositoryFullName => repository.FullName;
  public string RepositoryUrl => repository.HtmlUrl;
  public string  GitHubUsername => repository.Owner.Login;
}