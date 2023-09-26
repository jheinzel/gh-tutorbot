using System.CommandLine;
using Microsoft.Extensions.Logging;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
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

      var progress = new ProgressBar();
      var parameters = new AssigmentParameters(classroom.Id, assignmentName);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();

      var reviewStats = await assignment.GetReviewStatistics(studentList);

      var sortedReviewStats =
        sortOption switch
        {
          "reviewer" => reviewStats.OrderBy(rs => rs.Key.Reviewer),
          "review-date" => reviewStats.OrderByDescending(rs => rs.Value.LastReviewDate),
          "comment-length" => reviewStats.OrderByDescending(rs => rs.Value.NumComments),
          _ => throw new DomainException($"Unknown sort option \"{sortOption}\".")
        };

      sortedReviewStats = reviewStats.OrderBy(rs => rs.Key.Reviewer);

      foreach (var ((ownerName, reviewerName), stats) in sortedReviewStats)
      {
        if (! studentList.TryGetValue(ownerName, out var owner))
        {
          throw new DomainException($"Unknown student \"{ownerName}\".");
        }

        if (!studentList.TryGetValue(reviewerName, out var reviewer))
        {
          throw new DomainException($"Unknown student \"{reviewerName}\".");
        }

        printer.AddRow(reviewer.FullName, 
                       owner.FullName, 
                       stats.NumReviews.ToString().PadLeft(8), 
                       stats.NumComments.ToString().PadLeft(9),
                       stats.NumWords.ToString().PadLeft(6), 
                       stats.LastReviewDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
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

