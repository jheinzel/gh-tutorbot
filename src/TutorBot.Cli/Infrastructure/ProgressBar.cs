﻿using TutorBot.Infrastructure.TextWriterExtensions;

namespace TutorBot.Infrastructure;

public interface IProgress : IDisposable
{
  void Init(int max);
  void Increment(int increment = 1);
}

public class ProgressBar : IProgress
{
  private int max;
  private int progress;
  private int curserPosition;
  private ConsoleColor color;

  public ProgressBar(ConsoleColor color = ConsoleColor.Green, int max = 100)
  {
    this.color = color;
    Init(max);
  }

  public void Init(int max)
  {
    this.progress = 0;
    this.max = max;
    this.curserPosition = Console.CursorLeft;
  }

  public void Increment(int increment)
  {
    progress += increment;

    Console.CursorLeft = curserPosition;
    var progressPercent = (int)((double)progress / max * 100);
    var progressString = new string('#', progress);
    var remainingString = new string(' ', Math.Max(0, max - progress));

    Console.ForegroundColor = color;
    Console.Write($"[{progressString}{remainingString}] {progressPercent,3}%");
    Console.ResetColor();
  }

  public void Dispose()
  {
    Console.WriteLine();
  }
}