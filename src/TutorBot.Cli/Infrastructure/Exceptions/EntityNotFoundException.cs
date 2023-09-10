namespace TutorBot.Infrastructure.Exceptions;

public class EntityNotFoundException : InfrastrucureException
{
  public EntityNotFoundException(Type type, string name) : base($"{type.Name} with name \"{name}\" not found")
  {
  }
}
