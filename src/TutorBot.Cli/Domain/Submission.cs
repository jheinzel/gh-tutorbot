using Octokit;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;

namespace TutorBot.Domain;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public class Submission
{
  private IGitHubClassroomClient client;
  private Repository repository;
  
  public long RepositoryId => repository.Id;
  public string RepositoryName => repository.Name;
  public string RepositoryFullName => repository.FullName;
  public string RepositoryUrl => repository.HtmlUrl;
  public Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; }

  public Assessment Assessment { get; private set; } = new Assessment();

  public Submission(IGitHubClassroomClient client, Repository repository, Student owner, IEnumerable<Reviewer> reviewers)
  {
    this.client = client;
    this.repository = repository;
    this.Owner = owner;
    this.Reviewers = reviewers.ToList();
  }

  public async Task AddReviewStatistics(IStudentList students, ReviewStatistics reviewStats)
  {
    var reviews = await client.Repository.PullRequest.Review.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    foreach (var review in reviews.Where(r => students.Contains(r.User.Login)))
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
    foreach (var comment in comments.Where(r => students.Contains(r.User.Login)))
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

public class UnlinkedSubmission
{
  private Repository repository;

  public long RepositoryId => repository.Id;
  public string RepositoryName => repository.Name;
  public string RepositoryFullName => repository.FullName;
  public string RepositoryUrl => repository.HtmlUrl;
  public string  GitHubUsername => repository.Owner.Login;

  public UnlinkedSubmission(Repository repository) => this.repository = repository;
}