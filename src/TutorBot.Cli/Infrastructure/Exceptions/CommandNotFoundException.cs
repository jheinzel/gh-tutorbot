namespace TutorBot.Infrastructure.Exceptions;

public class CommandNotFoundException : InfrastrucureException
{
  public CommandNotFoundException(string commandName) : base($"Command \"{commandName}\" not found")
  {
  }
}
