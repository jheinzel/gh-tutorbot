using TutorBot.Infrastructure.TextWriterExtensions;

namespace TutorBot.Infrastructure;

public interface IProgress : IDisposable
{
  void Init(int max);
  void Increment(int increment = 1);
  void WriteLine(string message, TextWriter writer);
}

public class ProgressBar : IProgress
{
  private int max;
  private int progress;
  private int curserPosition;
  private int renderedLength;
  private string title;
  private ConsoleColor color;

  public ProgressBar(string title = "", ConsoleColor color = ConsoleColor.Green, int max = 100)
  {
    this.title = title;
    this.color = color;
    Init(max);
  }

  public void Init(int max)
  {
    this.progress = 0;
    this.max = max;
    this.curserPosition = Console.CursorLeft;
    this.renderedLength = 0;
  }

  public void Increment(int increment)
  {
    progress += increment;

    if (!Console.IsOutputRedirected)
    {
      Render();
    }
  }

  public void WriteLine(string message, TextWriter writer)
  {
    if (Console.IsOutputRedirected)
    {
      writer.WriteLine(message);
      return;
    }

    Console.CursorLeft = curserPosition;
    Console.Write(new string(' ', renderedLength));
    Console.CursorLeft = curserPosition;
    writer.WriteLine(message);
    curserPosition = Console.CursorLeft;
    Render();
  }

  private void Render()
  {
    Console.CursorLeft = curserPosition;
    var progressPercent = (int)((double)progress / max * 100);
    var progressBarLength = (int)((double)progress / max * Constants.MAX_PROGESSBAR_LENGTH);
    var progressString = new string('█', progressBarLength);
    var remainingString = new string(' ', Math.Max(0, Constants.MAX_PROGESSBAR_LENGTH - progressBarLength));
    var progressText = $"{title}{(title == "" ? "" : ": ")}[{progressString}{remainingString}] {progressPercent,3}%";

    Console.ForegroundColor = color;
    Console.Write(progressText);
    Console.ResetColor();
    renderedLength = progressText.Length;
  }

  public void Dispose()
  {
    if (!Console.IsOutputRedirected)
    {
      Console.WriteLine();
    }
  }
}

public static class ProgressBarExtensions
{
  extension(IProgress? progress)
  {
    public void WriteLineAndRedrawProgress(string message, TextWriter writer)
    {
      if (progress is null)
      {
        writer.WriteLine(message);
      }
      else
      {
        progress.WriteLine(message, writer);
      }
    }
  }
}
