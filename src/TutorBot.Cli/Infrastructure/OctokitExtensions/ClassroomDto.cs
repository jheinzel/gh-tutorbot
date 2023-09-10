using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Infrastructure.OctokitExtensions;

public class ClassroomDto
{
  public long Id { get; init; }
  public string? Name { get; init; }
  public string? Url { get; init; }
  public bool Archived { get; init; }
}
