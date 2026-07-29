using System.CommandLine;
using System.ComponentModel;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class CloneSubmissionsCommand : ClassroomCommand
{
  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment slug" };
  private readonly Option<string> directoryOption = new("--directory") { Description = "directory repositories will be cloned to", Aliases = { "-d" } };

  private async Task HandleAsync(string assignmentSlug, string? classroomName, string? org, string? directory)
  {
    ValidateClassroomAndOrg(ref classroomName, ref org);

    try
    {
      directory ??= assignmentSlug;

      // check if directory does not exist or is empty
      if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
      {
        throw new DomainException($"Error: Directory \"{directory}\" already exists and is not empty.");
      }

      var studentList = await StudentList.FromGitHub(Client, org, classroomName);
      var classroom = await Client.Classroom.GetByName(classroomName, org);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentSlug, ClassroomName: classroomName, Org: org, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(Client, studentList, parameters, Configuration, progress);
      progress.Dispose();

      foreach (var submission in assignment.Submissions.Where(s => s.Assessment.IsValid())
                                                       .OrderBy(s => s.Owner.FullName))
      {
        try
        {
          var localDirName = submission.Owner.FullName.Replace(" ", "_").Replace(".", "");
          var ownerName = submission.Owner.FullName;
          var repoFullName = submission.RepositoryFullName;

          var (result, errorResult, exitCode) = await ProcessHelper.RunProcessAsync("gh", $"repo clone {repoFullName} {directory}/{localDirName}");

          if (exitCode == 0)
          {
            Console.WriteLine($"Cloned repository of \"{ownerName}\"");
          }
          else
          {
            var errorMessage = (errorResult ?? "").Trim().Indent(2);
            Console.Error.WriteLine($"Problems cloning repository of \"{ownerName}\":\n{errorMessage}");
          }
        }
        catch (Win32Exception)
        {
          throw new DomainException("Error: Command \"gh\" (GitHub CLI) not found.");
        }
      }
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public CloneSubmissionsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) : 
    base("clone-submissions", "Clone all repositories of an assignment", client, configuration)
  {
    SetupCommonOptionDefaults();

    Add(assignmentArgument);
    Options.Add(ClassroomOption);
    Options.Add(OrgOption);
    Options.Add(directoryOption);

    Aliases.Add("cs");

    SetAction(async parsedResult =>
    {
      var assignmentSlug = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetValue(ClassroomOption);
      var org = parsedResult.GetValue(OrgOption);
      var directory = parsedResult.GetValue(directoryOption);
      await HandleAsync(assignmentSlug, classroomName, org, directory);
    });
  }
}
