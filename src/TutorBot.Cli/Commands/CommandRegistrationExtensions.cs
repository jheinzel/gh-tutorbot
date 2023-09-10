using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TutorBot.Commands;

public static class CommandRegistrationExtensions
{
  public static void RegisterCommands(this IServiceCollection services, Assembly assembly)
  {
    var commandType = typeof(Command);
    var commandImplementations = assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && commandType.IsAssignableFrom(t));

    foreach (var implementationType in commandImplementations)
    {
      services.AddSingleton(commandType, implementationType);
    }
  }
}
