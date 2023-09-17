using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure.Exceptions;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Logic;
using TutorBot.Logic.Exceptions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListReviewStatisticsCommand : Command
{
  private readonly IGitHubClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName);

      foreach (var submission in assignment.Submissions)
      {
        Console.WriteLine($"{submission.Owner.FullName} ({submission.Owner.MatNr})");
        await submission.GetReviewStatistics(client, studentList,
          successAction: (reviewer, stats) => Console.WriteLine($"  {reviewer}: reviews/comments/words: {stats.NumReviews}/{stats.NumComments}/{stats.NumWords} ({stats.LastReviewDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm")})"));
      }
    }
    catch (Exception ex) when (ex is LogicException || ex is InfrastrucureException)
    {
      Console.Error.WriteRedLine($"{ex.Message}");
    }
  }

  public ListReviewStatisticsCommand(IGitHubClient client, ConfigurationHelper configuration, ILogger<ListAssignmentsCommand> logger) : 
    base("list-review-statistics", "Display summary of reviewers' activity")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);


    AddAlias("lr");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption);
  }
}

