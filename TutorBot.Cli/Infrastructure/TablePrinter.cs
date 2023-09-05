using System.ComponentModel;

namespace TutorBot.Infrastructure;

public class TablePrinter
{
  private IList<IList<string>> table = new List<IList<string>>();

  public void AddRow(params string?[] columns)
  {
    table.Add(columns.Select(c => c ?? "").ToList());
  }

  public void Print()
  {
    if (table.Count == 0)
    {
      return;
    } 

    var columnWidths = new int[table[0].Count];
    foreach (var row in table)
    {
      for (int i = 0; i < row.Count; i++)
      {
        columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
      }
    }

    foreach (var row in table)
    {
      for (int i = 0; i < row.Count; i++)
      {
        Console.Write(row[i].PadRight(columnWidths[i] + 2));
      }
      Console.WriteLine();
    }
  }
}
