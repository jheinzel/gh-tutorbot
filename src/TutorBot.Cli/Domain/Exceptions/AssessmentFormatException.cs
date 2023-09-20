using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Logic.Exceptions;

public class AssessmentFormatException : LogicException
{
  public AssessmentFormatException(string message) : base(message)
  {
  }
}
