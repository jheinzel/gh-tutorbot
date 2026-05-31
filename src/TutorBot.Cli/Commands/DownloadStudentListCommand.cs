using System.CommandLine;
using Octokit;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class DownloadStudentListCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Option<string> classroomOption = new("--classroom") { Description = "classroom name", Aliases = { "-c" } };
  private readonly Option<string> orgOption = new("--org") { Description = "GitHub organization", Aliases = { "-o" } };

  private async Task HandleAsync(string classroomName, string org)
  {
    try
    {
      var repository = await client.Repository.Get(org, Constants.CLASSROOM_METADATA_REPO_NAME);
      var contentList = await client.Repository.Content.GetAllContents(repository.Id, $"{classroomName}/students.csv");
      var studentsContent = contentList.Single().Content;

      File.WriteAllText(Constants.ROSTER_FILE_PATH, studentsContent);
      Console.WriteLine($"Downloaded student list to \"{Constants.ROSTER_FILE_PATH}\".");
    }
    catch (NotFoundException)
    {
      throw new DomainException($"Error: Could not find student list at \"https://github.com/{org}/{Constants.CLASSROOM_METADATA_REPO_NAME}/blob/HEAD/{classroomName}/students.csv\".");
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public DownloadStudentListCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("download-student-list", "Download students.csv from classroom repository")
  {
    this.client = client;
    this.configuration = configuration;

    classroomOption.DefaultValueFactory = _ => configuration.DefaultClassroom;
    Options.Add(classroomOption);

    orgOption.DefaultValueFactory = _ => configuration.DefaultOrganization;
    Options.Add(orgOption);

    Aliases.Add("dsl");

    SetAction(async parsedResult =>
    {
      var classroomName = parsedResult.GetRequiredValue(classroomOption);
      var org = parsedResult.GetRequiredValue(orgOption);
      await HandleAsync(classroomName, org);
    });
  }
}
