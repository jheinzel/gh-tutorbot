﻿using System.Globalization;
using System.Text.RegularExpressions;
using Octokit;
using TutorBot.Domain;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public enum AssessmentState
{
  Loaded,
  NotLoaded,
  NotFound,
  InvalidFormat
}

public class Submission
{
  private IGitHubClient client;
  private Repository repository;
  
  public long RepositoryId => repository.Id;
  public string RepositoryName => repository.Name;
  public string RepositoryFullName => repository.FullName;
  public string RepositoryUrl => repository.HtmlUrl;
  public Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; } = new List<Reviewer>();

  public Assessment? Assessment { get; private set; }
  public AssessmentState AssessmentState { get; private set; } = AssessmentState.NotLoaded;

  public Submission(IGitHubClient client, Repository repository, Student owner)
  {
    this.client = client;
    this.repository = repository;
    this.Owner = owner;
  }

  public async Task LoadAssessment()
  {
    try
    {
      var contentList = await client.Repository.Content.GetAllContents(repository.Id, Constants.ASSESSMENT_FILE_NAME);
      Assessment = Assessment.FromString(contentList.Single().Content);
      AssessmentState = AssessmentState.Loaded;
    }
    catch (Octokit.NotFoundException)
    {
      AssessmentState = AssessmentState.NotFound;
    }
    catch (AssessmentFormatException)
    {
      AssessmentState = AssessmentState.InvalidFormat;
    }
  }

  public async Task AddReviewStatistics(StudentList students, ReviewStatistics reviewStats)
  {
    var reviews = await client.Repository.PullRequest.Review.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    foreach (var review in reviews.Where(r => students.Contains(r.User.Login)))
    {
      if (!reviewStats.TryGetValue((Owner.GitHubUsername, review.User.Login), out Logic.ReviewStatisticsItem? stats))
      {
        stats = new Logic.ReviewStatisticsItem();
        reviewStats.Add((Owner.GitHubUsername, review.User.Login), stats);
      }

      var reviewDate = review.SubmittedAt;
      if (reviewDate > stats.LastReviewDate)
      {
        stats.LastReviewDate = reviewDate;
      }

      stats.NumReviews++;
      stats.NumWords += review.Body.WordCount();
    }

    var comments = await client.Repository.PullRequest.ReviewComment.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    foreach (var comment in comments.Where(r => students.Contains(r.User.Login)))
    {
      if (!reviewStats.TryGetValue((Owner.GitHubUsername, comment.User.Login), out Logic.ReviewStatisticsItem? stats))
      {
        stats = new Logic.ReviewStatisticsItem();
        reviewStats.Add((Owner.GitHubUsername, comment.User.Login), stats);
      }

      stats.NumComments++;
      stats.NumWords += comment.Body.WordCount();
    }

    reviewStats.Remove((Owner.GitHubUsername, Owner.GitHubUsername));
  }
}
