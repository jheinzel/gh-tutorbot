using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Logic.Exceptions;

public class AssessmentFileException : LogicException
{
    public AssessmentFileException(string message) : base(message)
    {
    }
}
