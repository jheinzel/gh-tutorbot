namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IAssignmentsClient
{
  Task<IReadOnlyList<AssignmentDto>> GetAll(long classroomId);
  Task<AssignmentDto> GetByName(long classroomId, string assignmentName);
}
