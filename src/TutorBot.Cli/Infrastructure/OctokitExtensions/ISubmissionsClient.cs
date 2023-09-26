namespace TutorBot.Infrastructure.OctokitExtensions;

public interface ISubmissionsClient
{
  Task<IReadOnlyList<SubmissionDto>> GetAll(long assignmentId, IProgress? progress = null);
}
