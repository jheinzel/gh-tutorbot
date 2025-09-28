using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace TutorBot;

public class App(IServiceProvider serviceProvider)
{
  private readonly IServiceProvider serviceProvider = serviceProvider;

  public async Task RunAsync(string[] args)
  {
    var rootCommand = new RootCommand("TutorBot CLI");

    foreach (var command in serviceProvider.GetServices<Command>())
    {
      rootCommand.Add(command);
    }

    var parseResult = rootCommand.Parse(args);
    await parseResult.InvokeAsync();
  }
}