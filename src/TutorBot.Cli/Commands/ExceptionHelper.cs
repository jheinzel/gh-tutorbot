using Octokit;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Commands;

public static class ExceptionHelper
{
  public static void HandleException(Exception ex)
  {
    switch (ex)
    {
      case DomainException or InfrastrucureException:
        Console.Error.WriteRedLine($"{ex.Message}");
        break;

      case ApiException apiEx:
        Console.Error.WriteRedLine($"HTTP {(int)apiEx.StatusCode}: {apiEx.Message} ({apiEx.ApiError.DocumentationUrl})");
        break;

      default:
        throw ex; // rethrow unexpected exceptions
    }
  }
}
