namespace TutorBot.Domain.Exceptions;

public class ReviewerAssignmentException : DomainException
{
  public ReviewerAssignmentException(string message) : base(message)
  {
  }
}
