namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IClassroomsClient
{
  Task<IReadOnlyList<ClassroomDto>> GetAll(string org);
  Task<ClassroomDto> GetById(long classroomId);
  Task<ClassroomDto> GetByName(string classroomName, string? org = null);
  IAssignmentsClient Assignment { get; }
  ISubmissionsClient Submissions { get; }
}
