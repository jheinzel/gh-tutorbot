namespace TutorBot.Infrastructure.TextWriterExtensions;

public static class TextWriteExtensions 
{
  extension(TextWriter writer)
  {
    public void WriteColoredLine(ConsoleColor color, string format, params object?[] args)
    {
      Console.ForegroundColor = ConsoleColor.Red;
      writer.WriteLine(format, args);
      Console.ResetColor();
    }

    public void WriteRedLine(string format, params object?[] args) => writer.WriteColoredLine(ConsoleColor.Red, format, args);

    public void WriteGreenLine(string format, params object?[] args) => writer.WriteColoredLine(ConsoleColor.Green, format, args);

    public void WriteBlueLine(string format, params object?[] args) => writer.WriteColoredLine(ConsoleColor.Blue, format, args);
  }

}
