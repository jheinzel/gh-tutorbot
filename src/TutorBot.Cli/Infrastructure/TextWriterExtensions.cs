namespace TutorBot.Infrastructure.TextWriterExtensions;

public static class TextWriteExtensions 
{
  public static void WriteColoredLine(this TextWriter writer, ConsoleColor color, string format, params object?[] args)
  {
    Console.ForegroundColor = ConsoleColor.Red;
    writer.WriteLine(format, args);
    Console.ResetColor();
  }


  public static void WriteRedLine(this TextWriter writer, string format, params object?[] args) => WriteColoredLine(writer, ConsoleColor.Red, format, args);

  public static void WriteGreenLine(this TextWriter writer, string format, params object?[] args) => WriteColoredLine(writer, ConsoleColor.Green, format, args);

  public static void WriteBlueLine(this TextWriter writer, string format, params object?[] args) => WriteColoredLine(writer, ConsoleColor.Blue, format, args);

}
