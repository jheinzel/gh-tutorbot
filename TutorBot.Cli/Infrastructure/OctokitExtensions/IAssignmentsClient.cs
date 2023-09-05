using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IAssignmentsClient
{
  Task<IReadOnlyList<AssignmentDto>> GetAll(long classroomId);
  Task<AssignmentDto> GetByName(long classroomId, string assignmentName);
}
