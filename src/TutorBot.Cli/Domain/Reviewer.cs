namespace TutorBot.Domain;

public class Reviewer : Student
{
  public int? InvitationId { get; init; }

  public Reviewer(Student student, int? invitationId = null) : base(student.GitHubUsername, student.LastName, student.FirstName, student.MatNr, student.GroupNr)
  {
    InvitationId = invitationId;
  }

  public bool IsInvitationPending => InvitationId.HasValue;

  public override string FullName => $"{base.FullName}{(IsInvitationPending ? " (P)" : "")}";
}
