namespace TutorBot.Infrastructure;
public static class MathExtensions
{
  public const double EPSILON = 0.0001;

  public static bool IsInRange(this double value, double min, double max)
  {
    return value >= min - EPSILON && value <= max + EPSILON;
  }

  public static bool IsPositive(this double value)
  {
    return value.IsGreaterThan(0);
  }

  public static bool IsZero(this double value)
  {
    return Math.Abs(value) < EPSILON;
  }

  public static bool IsGreaterThan(this double value, double threshold)
  {
    return value > threshold + EPSILON;
  }
}
