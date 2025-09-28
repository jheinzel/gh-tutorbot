using System.Text.Json;
using System.Text.Json.Serialization;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Domain.JPlag;

public class JplagTopComparisonsDocument
{
  public static async Task<List<TopComparison>> FromJsonAsync(Stream jsonStream)
  {
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    return (await JsonSerializer.DeserializeAsync<List<TopComparison>>(jsonStream, options)) ?? throw new DomainException("Could not deserialize JSON");
  }
}

public class TopComparison
{
	[JsonPropertyName("firstSubmission")]
	public string? FirstSubmission { get; set; }

	[JsonPropertyName("secondSubmission")]
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