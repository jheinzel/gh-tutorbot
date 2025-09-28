using System.CommandLine;
using Microsoft.Extensions.Logging;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class ListReviewStatisticsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment") { Description = "assignment name" };
  private readonly Option<string> classroomOption = new("--classroom") { Description = "classroom name", Aliases = { "-c" } };
  private readonly Option<string> orderOption = new("--order-by") { Description = "order criteria", Aliases = { "-o" } };
  private readonly Option<int?> groupOption = new("--group") { Description = "filter group", Aliases = { "-g" } };
  private readonly Option<bool> allReviewersOption = new("--all-reviewers") { Description = "include review statistics from non-students", Aliases = { "-a" } };

  private async Task HandleAsync(string assignmentName, string classroomName, string order, int? group, bool showAllReviewers)
  {
    var printer = new TablePrinter();
    printer.AddRow("REVIEWER", "GR.", "OWNER", "#REV.", "#COMM.", "#WORDS", "LASTREVIEWDATE", "REVIEW URL");

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();

      var progressStatistics = new ProgressBar("Loading statistics ");
      var reviewStats = await assignment.GetReviewStatistics(progressStatistics);
      progressStatistics.Dispose();

      var orderedReviewStats =
        order switch
        {
          "reviewer" => reviewStats.OrderBy(rs => rs.Key.Reviewer),
          "review-date" => reviewStats.OrderBy(rs => rs.Value.LastReviewDate ?? DateTimeOffset.MaxValue),
          "review-date-desc" => reviewStats.OrderByDescending(rs => rs.Value.LastReviewDate ?? DateTimeOffset.MaxValue),
          "comment-length" => reviewStats.OrderBy(rs => rs.Value.NumComments),
          "comment-length-desc" => reviewStats.OrderByDescending(rs => rs.Value.NumComments),
          _ => throw new DomainException($"Unknown order option \"{order}\".")
        };

      foreach (var ((ownerName, reviewerName), stats) in orderedReviewStats)
      {
        if (!studentList.TryGetValue(ownerName, out var owner))
        {
          Console.Out.WriteRedLine($"Ignoring repository from unknown student \"{ownerName}\".");
          continue;
        }

        if (!studentList.TryGetValue(reviewerName, out var reviewer) && !showAllReviewers)
        {
           continue; // ignore reviews from non-students when --all-reviewers is not specified
        }

        if (reviewer is null || group is null || reviewer.GroupNr == group.Value)
        {
          var lastReviewDate = stats.LastReviewDate?.ToString("yyyy-MM-dd HH:mm") ?? "-";
          var reviewerDisplayName = reviewer is null ? reviewerName : reviewer.FullName;
          var reviewerGroup = reviewer is null ? "-" : reviewer.GroupNr.ToString();

          var submission = assignment.Submissions.FirstOrDefault(s => s.Owner == owner);
          var pullRequestUrl = submission is not null ?
                                $"{submission.RepositoryUrl}/pull/{Constants.FEEDBACK_PULLREQUEST_ID}" :
                                "-";

          printer.AddRow(reviewerDisplayName,
                         reviewerGroup.PadLeft(3),
                         owner.FullName,
                         stats.NumReviews.ToString().PadLeft(8),
                         stats.NumComments.ToString().PadLeft(9),
                         stats.NumWords.ToString().PadLeft(6),
                         lastReviewDate,
                         pullRequestUrl);
        }
      }

      printer.Print();
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public ListReviewStatisticsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("list-review-statistics", "Display summary of reviewers' activity")
  {
    this.client = client;
    this.configuration = configuration;

    Add(assignmentArgument);

    classroomOption.DefaultValueFactory = _ => configuration.DefaultClassroom;
    Options.Add(classroomOption);

    orderOption.AcceptOnlyFromAmong("reviewer", "comment-length", "comment-length-desc", "review-date", "review-date-desc");
    orderOption.DefaultValueFactory = _ => "review-date-desc";
    Options.Add(orderOption);

    groupOption.DefaultValueFactory = _ => null;
    Options.Add(groupOption);

    allReviewersOption.DefaultValueFactory = _ => false;
    Options.Add(allReviewersOption);

    Aliases.Add("lr");

    SetAction(async parsedResult =>
    {
      var assignmentName = parsedResult.GetRequiredValue(assignmentArgument);
      var classroomName = parsedResult.GetRequiredValue(classroomOption);
      var order = parsedResult.GetRequiredValue(orderOption);
      var group = parsedResult.GetValue(groupOption);
      var showAllReviewers = parsedResult.GetValue(allReviewersOption);
      await HandleAsync(assignmentName, classroomName, order, group, showAllReviewers);
    });
  }
}

