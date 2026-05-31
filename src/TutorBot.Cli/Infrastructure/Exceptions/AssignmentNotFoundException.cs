namespace TutorBot.Infrastructure.Exceptions;

public class AssignmentNotFoundException : InfrastrucureException
{
  public AssignmentNotFoundException(string slug) : base($"Assignment \"{slug}\" does not exist.")
  {
  }
}
