using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Logic;

public class Submission
{
  public required string RepositoryName { get; init; }
  public required Student Student { get; init; }

  public IList<Student> Reviewers { get; set; } = new List<Student>();
}
