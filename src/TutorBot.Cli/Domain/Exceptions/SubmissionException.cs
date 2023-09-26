namespace TutorBot.Domain.Exceptions;

public class SubmissionException : DomainException
{
  public SubmissionException(string message) : base(message)
  {
  }
}
