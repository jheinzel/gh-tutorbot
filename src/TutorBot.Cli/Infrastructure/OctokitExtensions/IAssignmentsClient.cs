namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IAssignmentsClient
{
  Task<IReadOnlyList<AssignmentDto>> GetAll(long classroomId);
  Task<AssignmentDto> GetBySlug(long classroomId, string assignmentSlug);

  Task<IReadOnlyList<AssignmentDto>> GetAll(string org, string classroomName);
  Task<AssignmentDto> GetBySlug(string org, string classroomName, string assignmentSlug);
}
