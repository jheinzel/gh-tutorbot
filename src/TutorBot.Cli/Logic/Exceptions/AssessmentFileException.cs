using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Logic.Exceptions;

public class AssessmentFileException : InfrastrucureException
{
    public AssessmentFileException(string message) : base(message)
    {
    }
}
