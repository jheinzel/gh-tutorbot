using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
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
  private readonly Option<string> sortOption = new("--sort-by", "sorting criteria");

  private async Task HandleAsync(string assignmentName, string classroomName, string sortOption)
  {
    var printer = new TablePrinter();
    printer.AddRow("REVIEWER", "OWNER", "#REVIEWS", "#COMMENTS", "#WORDS", "LASTREVIEWDATE");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom().GetByName(classroomName);
      var assignment = await Assignment.FromGitHub(client, studentList, classroom.Id, assignmentName);

      var reviewStats = await assignment.GetReviewStatistics(studentList);

      var sortedReviewStats =
        sortOption switch
        {
          "reviewer" => reviewStats.OrderBy(rs => rs.Key.Reviewer),
          "review-date" => reviewStats.OrderByDescending(rs => rs.Value.LastReviewDate),
          "comment-length" => reviewStats.OrderByDescending(rs => rs.Value.NumComments),
          _ => throw new LogicException($"Unknown sort option \"{sortOption}\".")
        };

      sortedReviewStats = reviewStats.OrderBy(rs => rs.Key.Reviewer);

      foreach (var ((ownerName, reviewerName), stats) in sortedReviewStats)
      {
        if (! studentList.TryGetValue(ownerName, out var owner))
        {
          throw new LogicException($"Unknown student \"{ownerName}\".");
        }

        if (!studentList.TryGetValue(reviewerName, out var reviewer))
        {
          throw new LogicException($"Unknown student \"{reviewerName}\".");
        }

        printer.AddRow(reviewer.FullName, owner.FullName, stats.NumReviews.ToString(), stats.NumComments.ToString(), stats.NumWords.ToString(), stats.LastReviewDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
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

    sortOption.AddAlias("-s");
    sortOption.FromAmong("reviewer", "comment-length", "review-date")
              .SetDefaultValue("reviewer");
    AddOption(sortOption);

    AddAlias("lr");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption, sortOption);
  }
}

