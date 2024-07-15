namespace TutorBot.Domain;

public class Reviewer(Student student, long? invitationId = null) : Student(student.GitHubUsername, student.LastName, student.FirstName, student.MatNr, student.GroupNr)
{
  public long? InvitationId { get; init; } = invitationId;

  public bool IsInvitationPending => InvitationId.HasValue;

  public override string FullName => $"{base.FullName}{(IsInvitationPending ? " (P)" : "")}";
}
