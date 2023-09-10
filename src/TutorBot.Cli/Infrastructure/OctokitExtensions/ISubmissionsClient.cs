using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace TutorBot.Infrastructure.OctokitExtensions;

public interface ISubmissionsClient
{
  Task<IReadOnlyList<SubmissionDto>> GetAll(long assignmentId);
}
