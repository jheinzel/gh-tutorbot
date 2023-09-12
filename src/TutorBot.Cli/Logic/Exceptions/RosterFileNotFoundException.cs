using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Logic.Exceptions;

public class RosterFileNotFoundException : InfrastrucureException
{
    public RosterFileNotFoundException(string message) : base(message)
    {
    }
}
