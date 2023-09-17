using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Octokit;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

public class ReviewStatistics {
  public int NumReviews { get; set; }
  public int NumComments { get; set; } 
  public int NumWords { get; set; }
  public DateTimeOffset LastReviewDate { get; set; }
}

public class Submission
{
  public required long RepositoryId { get; init; }
  public required string RepositoryName { get; init; }
  public required string RepositoryFullName { get; init; }
  public required string RepositoryUrl { get; init; }
  public required Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; } = new List<Reviewer>();

  public record AssessmentLine(string Exercise, int[] Gradings);


  public async Task<IReadOnlyList<AssessmentLine>> GetAssessment(IGitHubClient client)
  {
    try
    {
      var contentList = await client.Repository.Content.GetAllContents(RepositoryId, Constants.ASSESSMENT_FILE_NAME);
      return ParseMarkdownTable(contentList.Single().Content);
    }
    catch (Octokit.NotFoundException)
    {
      throw new AssessmentFileException($"\"{RepositoryName}\": No assessment file found in");
    }
  }

  public IReadOnlyList<AssessmentLine> ParseMarkdownTable(string content)
  {
    int FindTableIndex(string content)
    {
      for (int i = 0; i < content.Length; i++)
      {
        if (content[i] == '|')
        {
          return i;
        }
      }

      return -1;
    }

    int IgnoreLine(string content, int from)
    {
      int index = from;
      while (index < content.Length && content[index] != '\n')
      {
        index++;
      }

      return index + 1;
    }

    int ReadTableLine(string content, int from, out string[] values)
    {
      int index = from;
      while (index < content.Length && content[index] != '\n')
      {
        index++;
      }

      values = content[from..index].Split('|', StringSplitOptions.RemoveEmptyEntries);
      values = values.Select(value => value.Trim()).ToArray();

      return index + 1;
    }

    var assessmentList = new List<AssessmentLine>();

    int index = FindTableIndex(content);
    if (index == -1)
    {
      throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
    }

    index = IgnoreLine(content, index); // ignore header
    index = IgnoreLine(content, index); // ignore separator

    if (index == content.Length)
    {
      throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
    }

    while (index < content.Length)
    {
      index = ReadTableLine(content, index, out string[] values);
      if (values.Length != 4)
      {
        throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
      }

      int[] gradings = new int[values.Length - 1];
      for (int i = 1; i < values.Length; i++)
      {
        if (!int.TryParse(values[i], out gradings[i - 1]) || gradings[i - 1] < 0 || gradings[i - 1] > 100)
        {
          throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
        }
      }

      assessmentList.Add(new AssessmentLine(values[0], gradings));
    }

    return assessmentList.AsReadOnly();
  }

  public async Task GetReviewStatistics(IGitHubClient client, StudentList students, Action<string, ReviewStatistics> successAction)
  {
    var reviews = await client.Repository.PullRequest.Review.GetAll(RepositoryId, Constants.FEEDBACK_PULLREQUEST_ID);
    
    var userReviewStats = new Dictionary<string, ReviewStatistics>();

    foreach (var review in reviews)
    {
      ReviewStatistics? stats;
      if (!userReviewStats.TryGetValue(review.User.Login, out stats))
      {
        stats = new ReviewStatistics();
        userReviewStats.Add(review.User.Login, stats);
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
    foreach (var comment in comments)
    {
      ReviewStatistics? stats;
      if (!userReviewStats.TryGetValue(comment.User.Login, out stats))
      {
        stats = new ReviewStatistics();
        userReviewStats.Add(comment.User.Login, stats);
      }

      stats.NumComments++;
      stats.NumWords += comment.Body.WordCount();
    }

    userReviewStats.Remove(Owner.GitHubUsername);

    foreach (var (user, stats) in userReviewStats)
    {
      if (students.TryGetValue(user, out var student))
      {
        successAction(student.FullName, stats);
      }
      else
      {
        successAction(user, stats);
      }
    }

  }
}
