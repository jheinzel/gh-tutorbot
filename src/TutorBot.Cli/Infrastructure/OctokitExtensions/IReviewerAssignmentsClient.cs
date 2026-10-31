namespace TutorBot.Infrastructure.OctokitExtensions;

public interface IReviewerAssignmentsClient
{
  Task<ReviewerAssignmentsFile> Get(string org, string classroomName, string assignmentSlug);
  Task Save(string org, string classroomName, ReviewerAssignmentsFile reviewerAssignments);
}
