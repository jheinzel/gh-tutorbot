using Microsoft.Extensions.Configuration;

namespace TutorBot.Utility;
public class ConfigurationHelper
{
  private readonly IConfiguration configuration;

  public ConfigurationHelper(IConfiguration configuration)
  {
    this.configuration = configuration;
  }

  public const string KEY_DEFAULT_CLASSROOM = "default-classroom";
  private const string DEFAULT_CLASSROOM = "my-classroom";

  public string DefaultClassroom { get => configuration[KEY_DEFAULT_CLASSROOM] ?? DEFAULT_CLASSROOM; }

  public const string KEY_DEFAULT_ORGANIZATION = "default-organization";
  private const string DEFAULT_ORGANIZATION = "my-organization";

  public string DefaultOrganization { get => configuration[KEY_DEFAULT_ORGANIZATION] ?? DEFAULT_ORGANIZATION; }

  public const string KEY_JAVA_PATH = "java-path";
  private const string DEFAULT_JAVA_PATH = "java";

  public string JavaPath { get => configuration[KEY_JAVA_PATH] ?? DEFAULT_JAVA_PATH; }

  public const string KEY_JPLAG_JAR_PATH = "jplag-jar-path";
  private const string DEFAULT_JPLAG_JAR_PATH = @"./lib/jplag.jar";

  public string JplagJarPath { get => configuration[KEY_JPLAG_JAR_PATH] ?? DEFAULT_JPLAG_JAR_PATH; }

  public const string KEY_TEMPLATE_REPO_PATTERN = "template-repo-pattern";

  public string TemplateRepoPattern { get => configuration[KEY_TEMPLATE_REPO_PATTERN] ?? Constants.TEMPLATE_REPO_PATTERN; }
}