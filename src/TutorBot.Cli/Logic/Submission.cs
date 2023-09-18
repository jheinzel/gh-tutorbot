using System.Globalization;
using System.Text.RegularExpressions;
using Octokit;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

using ReviewStatistics = IDictionary<(string Owner, string Reviewer), ReviewStatisticsItem>;

public record AssessmentLine(string Exercise, int Weight, int[] Gradings);

public record Assessment(double Effort, IReadOnlyList<AssessmentLine> Lines);

public class Submission
{
  public required long RepositoryId { get; init; }
  public required string RepositoryName { get; init; }
  public required string RepositoryFullName { get; init; }
  public required string RepositoryUrl { get; init; }
  public required Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; } = new List<Reviewer>();

 
  public async Task<Assessment> GetAssessment(IGitHubClient client)
  {
    try
    {
      var contentList = await client.Repository.Content.GetAllContents(RepositoryId, Constants.ASSESSMENT_FILE_NAME);
      return ParseMarkdownTable(contentList.Single().Content);
    }
    catch (Octokit.NotFoundException)
    {
      throw new AssessmentFileException($"\"{RepositoryName}\": No assessment file found");
    }
  }

  public Assessment ParseMarkdownTable(string content)
  {
    int FindTableIndex(string content, int from)
    {
      for (int i = from; i < content.Length; i++)
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

    int ReadLine(string content, int from, out string line)
    {
      int index = from;
      while (index < content.Length && content[index] != '\n')
      {
        index++;
      }

      line = content[from..index];
      return index + 1;
    }

    int ReadTableLine(string content, int from, out string[] values)
    {
      int index = ReadLine(content, from, out string line);

      values = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
      values = values.Select(value => value.Trim()).ToArray();

      return index;
    }

    int ParseLabeledNumber(string content, string label, int from, out double number)
    {
      string pattern = $@"{label}[^:]*:\s*(?<number>[+-]?(\d*[.])?\d+)";

      number = 0;
      int index = from;
      while ((index = ReadLine(content, index, out string line)) < content.Length)
      {
        Match match = Regex.Match(line, pattern);
        if (!match.Success)
        {
          continue;
        }

        Group numberGroup = match.Groups["number"];
        if (double.TryParse(numberGroup.Value, CultureInfo.InvariantCulture, out number))
        {
          return index;
        }
        else
        {
          return -1;
        }
      }

      return -1;
    }

    var assessmentList = new List<AssessmentLine>();

    int index = ParseLabeledNumber(content, Constants.EFFORT_PREFIX, 0, out double effort);
    if (index == -1)
    {
      throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
    }

    index = FindTableIndex(content, index);
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
      if (values.Length != 5)
      {
        throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
      }

      int weight = 0;
      if (!int.TryParse(values[1], out weight) || weight < 0 || weight > 100)
      {
        throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
      }

      int[] gradings = new int[values.Length - 1];
      for (int i = 2; i < values.Length; i++)
      {
        if (!int.TryParse(values[i], out gradings[i - 2]) || gradings[i - 2] < 0 || gradings[i - 2] > 100)
        {
          throw new AssessmentFileException($"\"{RepositoryName}\": Invalid assessment file format");
        }
      }

      assessmentList.Add(new AssessmentLine(values[0], weight, gradings));
    }

    return new Assessment(effort, assessmentList.AsReadOnly());
  }

  public async Task AddReviewStatistics(IGitHubClient client, StudentList students, ReviewStatistics reviewStats)
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
