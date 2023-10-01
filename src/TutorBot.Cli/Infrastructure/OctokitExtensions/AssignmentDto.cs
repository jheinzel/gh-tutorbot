using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class AssignmentDto
{
  public long Id { get; init; }
	public string Title { get; init; } = string.Empty;
	public int Accepted { get; init; }
	public DateTimeOffset? Deadline { get; init; }
}
