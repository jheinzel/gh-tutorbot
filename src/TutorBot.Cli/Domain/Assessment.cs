using System.Globalization;
using System.Text.RegularExpressions;
using Octokit;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Domain;

public enum AssessmentState
{
  Loaded,
  NotLoaded,
  NotFound,
  InvalidFormat
}

public record AssessmentLine(string Exercise, double Weight, double[] Gradings);

public class Assessment
{
  public double Effort { get; init; }
  public IReadOnlyList<AssessmentLine> Lines { get; private set; } = new List<AssessmentLine>();
  public double Value { get; internal set; } = 100.0;
  public AssessmentState State { get; private set; } = AssessmentState.NotLoaded;

  public Assessment()
  {
  }

  public async Task Load(IGitHubClient client, long repositoryId)
  {
    try
    {
      var contentList = await client.Repository.Content.GetAllContents(repositoryId, Constants.ASSESSMENT_FILE_NAME);
      LoadFromString(contentList.Single().Content);
      State = AssessmentState.Loaded;
    }
    catch (Octokit.NotFoundException)
    {
      State = AssessmentState.NotFound;
    }
    catch (AssessmentFormatException)
    {
      State = AssessmentState.InvalidFormat;
    }
  }

  public void LoadFromString(string content)
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

    int index = ParseLabeledNumber(content, Constants.EFFORT_PREFIX, 0, out var Effort);
    if (index == -1)
    {
      throw new AssessmentFormatException($"Cannot parse effort entry");
    }

    index = FindTableIndex(content, index);
    if (index == -1)
    {
      throw new AssessmentFormatException($"Cannot find assessment table");
    }

    index = IgnoreLine(content, index); // ignore header
    index = IgnoreLine(content, index); // ignore separator

    if (index == content.Length)
    {
      throw new AssessmentFormatException($"Table not complete");
    }

    var lines = new List<AssessmentLine>();

    while (index < content.Length)
    {
      index = ReadTableLine(content, index, out string[] values);
      if (values.Length != 5)
      {
        throw new AssessmentFormatException($"Table row {lines.Count + 1} does not contain 5 rows");
      }

      double weight = 0;
      if (!double.TryParse(values[1], CultureInfo.InvariantCulture, out weight)
          || weight < 0 || weight > 100)
      {
        throw new AssessmentFormatException($"Invalid weight in table row {lines.Count + 1}");
      }

      var gradings = new double[values.Length - 1];
      for (int i = 2; i < values.Length; i++)
      {
        if (!double.TryParse(values[i], CultureInfo.InvariantCulture, out gradings[i - 2])
            || gradings[i - 2] < 0 || gradings[i - 2] > 100)
        {
          throw new AssessmentFormatException($"Invalid entry in column {i + 1} of row {lines.Count + 1}");
        }
      }

      lines.Add(new AssessmentLine(values[0], weight, gradings));
    }

    Lines = lines.AsReadOnly();
  }
}
