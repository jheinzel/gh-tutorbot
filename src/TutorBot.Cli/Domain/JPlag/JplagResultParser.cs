using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Domain.JPlag;


using System.Collections.Generic;
using System.Text.Json.Serialization;


public class JplagOverviewDocument
{
	public static async Task<JplagOverviewDocument> FromJsonAsync(Stream jsonStream)
	{
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		return (await JsonSerializer.DeserializeAsync<JplagOverviewDocument>(jsonStream, options)) ?? throw new DomainException("Could not deserialize JSON");
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
	public Dictionary<string, string> SubmissionIdToDisplayName { get; set; } = new();

	[JsonPropertyName("submission_ids_to_comparison_file_name")]
	public Dictionary<string, Dictionary<string, string>> SubmissionIdsToComparisonFileName { get; set; } = new();

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

	[JsonPropertyName("distributions")]
	public Distributions? Distributions { get; set; }

	[JsonPropertyName("top_comparisons")]
	public List<TopComparison> TopComparisons { get; set; } = new();

	[JsonPropertyName("clusters")]
	public List<object> Clusters { get; set; } = new();

	[JsonPropertyName("total_comparisons")]
	public int TotalComparisons { get; set; }
}

public class JplagVersion
{
	[JsonPropertyName("major")]
	public int Major { get; set; }

	[JsonPropertyName("minor")]
	public int Minor { get; set; }

	[JsonPropertyName("patch")]
	public int Patch { get; set; }
}

public class Distributions
{
	[JsonPropertyName("AVG")]
	public List<int> Avg { get; set; } = new();

	[JsonPropertyName("MAX")]
	public List<int> Max { get; set; } = new();
}

public class TopComparison
{
	[JsonPropertyName("first_submission")]
	public string? FirstSubmission { get; set; }

	[JsonPropertyName("second_submission")]
	public string? SecondSubmission { get; set; }

	[JsonPropertyName("similarities")]
	public Similarities? Similarities { get; set; }
}

public class Similarities
{
	[JsonPropertyName("AVG")]
	public double Avg { get; set; }

	[JsonPropertyName("MAX")]
	public double Max { get; set; }
}