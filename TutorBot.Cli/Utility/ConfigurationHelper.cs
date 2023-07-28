using Microsoft.Extensions.Configuration;

namespace TutorBot.Utility;
public class ConfigurationHelper
{
  private readonly IConfiguration configuration;

  public ConfigurationHelper(IConfiguration configuration)
  {
    this.configuration = configuration;
  }

  private const string DEFAULT_ORGANIZATION = "swo3";
  private const string KEY_DEFAULT_ORGANIZATION = "default-organization";

  public string DefaultOrganization { get => configuration[KEY_DEFAULT_ORGANIZATION] ?? DEFAULT_ORGANIZATION; }
}
