using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IClassroomsClient
{
  Task<IReadOnlyList<ClassroomDto>> GetAll();
  Task<ClassroomDto> GetById(long classroomId);
  Task<ClassroomDto> GetByName(string classroomName);
  IAssignmentsClient Assignment { get; }
  ISubmissionsClient Submissions { get; }
}
