using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Domain.Exceptions;

public class NonUniqueValuesException<T> : DomainException
{
  public IEnumerable<T> NonUniqueValues { get; }

  public NonUniqueValuesException(string message, IEnumerable<T> nonUniqueValues) : base(message)
  {
    NonUniqueValues = nonUniqueValues;
  }
}
