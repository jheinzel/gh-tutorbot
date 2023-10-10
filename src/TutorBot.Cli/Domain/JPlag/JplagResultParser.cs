using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Domain.JPlag;


public class JplagVersion
{
  [JsonPropertyName("major")]
  public int Major { get; set; }

  [JsonPropertyName("minor")]
  public int Minor { get; set; }

  [JsonPropertyName("patch")]
  public int Patch { get; set; }
}

public class JPlagComparison
{
  [JsonPropertyName("first_submission")]
  public string? FirstSubmission { get; set; }

  [JsonPropertyName("second_submission")]
  public string? SecondSubmission { get; set; }

  [JsonPropertyName("similarity")]
  public double Similarity { get; set; }
}

public class JPlagMetric
{
  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("distribution")]
  public List<int> Distribution { get; set; } = new();

  [JsonPropertyName("topComparisons")]
  public List<JPlagComparison> TopComparisons { get; set; } = new();

  [JsonPropertyName("description")]
  public string? Description { get; set; }
}

public class Cluster
{
  [JsonPropertyName("average_similarity")]
  public double AverageSimilarity { get; set; }

  [JsonPropertyName("strength")]
  public double Strength { get; set; }

  [JsonPropertyName("members")]
  public List<string> Members { get; set; } = new();
}

public class JPlagOverviewDocument
{
  public static async Task<JPlagOverviewDocument> FromJsonAsync(Stream jsonStream)
  {
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    return (await JsonSerializer.DeserializeAsync<JPlagOverviewDocument>(jsonStream, options)) ?? throw new DomainException("Could not deserialize JSON");
  }

  [JsonPropertyName("jplag_version")]
  public JplagVersion? JplagVersion { get; set; }

  [JsonPropertyName("submission_folder_path")]
  public List<string> SubmissionFolderPath { get; set; } = new();

  [JsonPropertyName("base_code_folder_path")]
  public string? BaseCodeFolderPath { get; set; }

  [JsonPropertyName("language")]
  public string? Language { get; set; }

  [JsonPropertyName("file_extensions")]
  public List<string> FileExtensions { get; set; } = new();

  [JsonPropertyName("submission_id_to_display_name")]
  public Dictionary<string, string>? SubmissionIdToDisplayName { get; set; }

  [JsonPropertyName("submission_ids_to_comparison_file_name")]
  public Dictionary<string, Dictionary<string, string>>? SubmissionIdsToComparisonFileName { get; set; }

  [JsonPropertyName("failed_submission_names")]
  public List<string> FailedSubmissionNames { get; set; } = new();

  [JsonPropertyName("excluded_files")]
  public List<string> ExcludedFiles { get; set; } = new();

  [JsonPropertyName("match_sensitivity")]
  public int MatchSensitivity { get; set; }

  [JsonPropertyName("date_of_execution")]
  public string? DateOfExecution { get; set; }

  [JsonPropertyName("execution_time")]
  public int ExecutionTime { get; set; }

  [JsonPropertyName("metrics")]
  public List<JPlagMetric> Metrics { get; set; } = new();

  [JsonPropertyName("clusters")]
  public List<Cluster> Clusters { get; set; } = new();

  [JsonPropertyName("total_comparisons")]
  public int TotalComparisons { get; set; }
}
