using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Octokit;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

public class Submission
{
  public required long RepositoryId { get; init; }
  public required string RepositoryName { get; init; }
  public required string RepositoryFullName { get; init; }
  public required string RepositoryUrl { get; init; }
  public required Student Owner { get; init; }

  public IList<Reviewer> Reviewers { get; set; } = new List<Reviewer>();

  public record AssessmentLine(string Exercise, int Weight, int[] Gradings);


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

    return assessmentList.AsReadOnly();
  }
}
