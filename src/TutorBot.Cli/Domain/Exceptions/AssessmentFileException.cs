using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Domain.Exceptions;

public class AssessmentFileException : DomainException
{
    public AssessmentFileException(string message) : base(message)
    {
    }
}
