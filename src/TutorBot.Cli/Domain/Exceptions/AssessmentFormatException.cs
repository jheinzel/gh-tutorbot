using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Domain.Exceptions;

public class AssessmentFormatException : DomainException
{
  public AssessmentFormatException(string message) : base(message)
  {
  }
}
