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
        var documentationUrl = apiEx.ApiError?.DocumentationUrl;
        var documentationSuffix = string.IsNullOrWhiteSpace(documentationUrl) ? string.Empty : $" ({documentationUrl})";
        Console.Error.WriteRedLine($"HTTP {(int)apiEx.StatusCode}: {apiEx.Message}{documentationSuffix}");
        break;

      default:
        throw ex; // rethrow unexpected exceptions
    }
  }
}
