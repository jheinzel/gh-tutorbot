using System.CommandLine;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TutorBot.Commands;

public static class CommandRegistrationExtensions
{
  extension(IServiceCollection services)
  {
    public void RegisterCommands(Assembly assembly)
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
}
