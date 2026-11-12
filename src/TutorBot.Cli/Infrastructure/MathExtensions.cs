namespace TutorBot.Infrastructure;
public static class MathExtensions
{
  public const double EPSILON = 0.0001;

  extension(double value)
  {
    public bool IsInRange(double min, double max)
    {
      return value >= min - EPSILON && value <= max + EPSILON;
    }

    public bool IsPositive()
    {
      return value.IsGreaterThan(0);
    }

    public bool IsZero()
    {
      return Math.Abs(value) < EPSILON;
    }

    public bool IsGreaterThan(double threshold)
    {
      return value > threshold + EPSILON;
    }
  }
}
