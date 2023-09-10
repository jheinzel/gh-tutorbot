using Microsoft.Extensions.Configuration;

namespace TutorBot.Utility;
public class ConfigurationHelper
{
  private readonly IConfiguration configuration;

  public ConfigurationHelper(IConfiguration configuration)
  {
    this.configuration = configuration;
  }

  private const string DEFAULT_CLASSROOM = "swo3";
  private const string KEY_DEFAULT_CLASSROOM = "default-classroom";

  public string DefaultClassroom { get => configuration[KEY_DEFAULT_CLASSROOM] ?? DEFAULT_CLASSROOM; }
}
