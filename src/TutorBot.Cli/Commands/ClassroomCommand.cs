using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

/// <summary>
/// Base class for commands that work with classrooms and GitHub organizations.
/// Provides common options (--classroom, --org), dependencies (client, configuration),
/// and validation methods for classroom/org requirements.
/// </summary>
internal abstract class ClassroomCommand : Command
{
  /// <summary>
  /// GitHub classroom client for API interactions.
  /// </summary>
  protected readonly IGitHubClassroomClient Client;

  /// <summary>
  /// Configuration helper for accessing default settings.
  /// </summary>
  protected readonly ConfigurationHelper Configuration;

  /// <summary>
  /// Option for specifying classroom name.
  /// </summary>
  protected readonly Option<string> ClassroomOption = new("--classroom")
  {
    Description = "classroom name",
    Aliases = { "-c" }
  };

  /// <summary>
  /// Option for specifying GitHub organization.
  /// </summary>
  protected readonly Option<string> OrgOption = new("--org")
  {
    Description = "GitHub organization",
    Aliases = { "-o" }
  };

  /// <summary>
  /// Initializes a new instance of the ClassroomCommand class.
  /// </summary>
  /// <param name="name">The name of the command.</param>
  /// <param name="description">The description of the command.</param>
  /// <param name="client">The GitHub classroom client.</param>
  /// <param name="configuration">The configuration helper.</param>
  protected ClassroomCommand(string name, string description, IGitHubClassroomClient client, ConfigurationHelper configuration)
    : base(name, description)
  {
    Client = client;
    Configuration = configuration;
  }

  /// <summary>
  /// Sets up default factories for classroom and org options based on configuration.
  /// This method should be called during command initialization to ensure defaults are available.
  /// </summary>
  protected void SetupCommonOptionDefaults()
  {
    ClassroomOption.DefaultValueFactory = _ => Configuration.DefaultClassroom;
    OrgOption.DefaultValueFactory = _ => Configuration.DefaultOrganization;
  }

  /// <summary>
  /// Validates that classroom and organization names are provided either via options or configuration defaults.
  /// Throws ArgumentException if either value is null or whitespace.
  /// After this method returns successfully, both parameters are guaranteed to be non-null.
  /// </summary>
  /// <param name="classroomName">The classroom name to validate.</param>
  /// <param name="org">The organization name to validate.</param>
  /// <exception cref="ArgumentException">Thrown when classroom or org is null or whitespace.</exception>
  protected static void ValidateClassroomAndOrg([NotNull] ref string? classroomName, [NotNull] ref string? org)
  {
    if (string.IsNullOrWhiteSpace(classroomName))
    {
      throw new ArgumentException("Classroom name is required. Provide --classroom or set default-classroom in configuration.", nameof(classroomName));
    }

    if (string.IsNullOrWhiteSpace(org))
    {
      throw new ArgumentException("Organization is required. Provide --org or set default-organization in configuration.", nameof(org));
    }
  }

  /// <summary>
  /// Validates that organization name is provided either via options or configuration defaults.
  /// Throws ArgumentException if the value is null or whitespace.
  /// After this method returns successfully, the parameter is guaranteed to be non-null.
  /// </summary>
  /// <param name="org">The organization name to validate.</param>
  /// <exception cref="ArgumentException">Thrown when org is null or whitespace.</exception>
  protected static void ValidateOrg([NotNull] ref string? org)
  {
    if (string.IsNullOrWhiteSpace(org))
    {
      throw new ArgumentException("Organization is required. Provide --org or set default-organization in configuration.", nameof(org));
    }
  }
}
