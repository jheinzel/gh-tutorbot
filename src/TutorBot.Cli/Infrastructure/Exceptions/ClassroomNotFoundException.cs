namespace TutorBot.Infrastructure.Exceptions;

public class ClassroomNotFoundException : InfrastrucureException
{
  public ClassroomNotFoundException(string name) : base($"Classroom with name \"{name}\" does not exist.")
  {
  }
}
