using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace TutorBot;

public class App
{
  private readonly IServiceProvider serviceProvider;

  public App(IServiceProvider serviceProvider)
  {
    this.serviceProvider = serviceProvider;
  }


  public async Task RunAsync(string[] args)
  {
    var rootCommand = new RootCommand();

    foreach (var command in serviceProvider.GetServices<Command>())
    {
      rootCommand.Add(command);
    }

    await rootCommand.InvokeAsync(args);
  }
}

