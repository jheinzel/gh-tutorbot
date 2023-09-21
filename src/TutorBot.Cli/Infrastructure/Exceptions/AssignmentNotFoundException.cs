namespace TutorBot.Infrastructure.Exceptions;

public class AssignmentNotFoundException : InfrastrucureException
{
  public AssignmentNotFoundException(string name) : base($"Assignment with name \"{name}\" does not exist.")
  {
  }
}
