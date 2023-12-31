﻿using System.CommandLine;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class CloneSubmissionsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");
  private readonly Option<string> directoryOption = new("--directory", "directory repositories will be cloned to");

  private async Task HandleAsync(string assignmentName, string classroomName, string? directory)
  {
    try
    {
      directory ??= assignmentName;

      // check if directory does not exist or is empty
      if (Directory.Exists(directory) && Directory.EnumerateFileSystemEntries(directory).Any())
      {
        throw new DomainException($"Error: Directory \"{directory}\" already exists and is not empty.");
      }

      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
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
    base("clone-submissions", "Clone all repositories of an assignment")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    directoryOption.AddAlias("-d");
    directoryOption.SetDefaultValue(null);
    AddOption(directoryOption);

    AddAlias("cs");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption, directoryOption);
  }
}

