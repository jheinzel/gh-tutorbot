using System.CommandLine;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.TextWriterExtensions;
using TutorBot.Utility;

namespace TutorBot.Commands;

internal class DownloadAssessmentsCommand : Command
{
  private readonly IGitHubClassroomClient client;
  private readonly ConfigurationHelper configuration;

  private readonly Argument<string> assignmentArgument = new("assignment", "assignment name");
  private readonly Option<string> classroomOption = new("--classroom", "classroom name");

  private async Task HandleAsync(string assignmentName, string classroomName)
  {
    void WriteHeader(IXLWorksheet worksheet, int row, IEnumerable<string> exercises)
    {
      int column = 1;
      foreach (var colName in new[] { "Name", "Mat.Nr", "Aufwand" })
      {
        worksheet.Cell(row, column++).Value = colName;
      }

      foreach (var exercise in exercises)
      {
        foreach (var gradingType in new[] { "L", "I", "T" })
        {
          worksheet.Cell(row, column++).Value = $"{exercise} - {gradingType}";
        }
      }
    }

    void WriteRow(IXLWorksheet worksheet, int row, string fullName, string matNr, double effort, IReadOnlyList<AssessmentLine> lines)
    {
      int column = 1;
      worksheet.Cell(row, column++).Value = fullName;
      worksheet.Cell(row, column++).Value = matNr;
      worksheet.Cell(row, column++).Value = effort;

      foreach (var line in lines)
      {
        foreach (var grading in line.Gradings)
        {
          worksheet.Cell(row, column++).Value = grading;
        }
      }
    }

    try
    {
      var studentList = await StudentList.FromRoster(Constants.ROSTER_FILE_PATH);
      var classroom = await client.Classroom.GetByName(classroomName);

      var progress = new ProgressBar("Loading submissions");
      var parameters = new AssigmentParameters(classroom.Id, assignmentName, LoadAssessments: true);
      var assignment = await Assignment.FromGitHub(client, studentList, parameters, progress);
      progress.Dispose();

      using var workbook = new XLWorkbook();
      var worksheet = workbook.AddWorksheet(assignment.Name);

      int i = 0;
      foreach (var submission in assignment.Submissions.Where(s => s.Assessment.IsValid())
                                                       .OrderBy(s => s.Owner.FullName))
      {
        try
        {
          var assessment = submission.Assessment ?? throw new InvalidOperationException($"Inconsitent Assessment state in sumbission {submission.RepositoryName}.");

          if (i == 0) // write header before first content row is written
          {
            WriteHeader(worksheet, i+1, assessment.Lines.Select(line => line.Exercise));
          }

          i++;
          WriteRow(worksheet, i+1, submission.Owner.FullName, submission.Owner.MatNr, assessment.Effort, assessment.Lines);
        }
        catch (AssessmentFileException ex)
        {
          Console.Error.WriteRedLine($"{ex.Message}");
        }
      }

      worksheet.Columns(1,2).AdjustToContents();

      var rangeWithData = worksheet.RangeUsed();
      var excelTable = rangeWithData.CreateTable();
      excelTable.Theme = XLTableTheme.TableStyleLight10;

      var assessmentsFileName = string.Format(Constants.ASSESSMENTS_DOWNLOAD_FILE_NAME, assignment.Name);
      workbook.SaveAs(assessmentsFileName);

      Console.WriteLine($"Downloaded {i} {(i==1 ? "assessment" : "assessments")} to \"{assessmentsFileName}\"");
    }
    catch (Exception ex)
    {
      ExceptionHelper.HandleException(ex);
    }
  }

  public DownloadAssessmentsCommand(IGitHubClassroomClient client, ConfigurationHelper configuration) :
    base("download-assessments", "Download assessments of all submissions")
  {
    this.client = client;
    this.configuration = configuration;

    AddArgument(assignmentArgument);

    classroomOption.AddAlias("-c");
    classroomOption.SetDefaultValue(configuration.DefaultClassroom);
    AddOption(classroomOption);

    AddAlias("da");

    this.SetHandler(HandleAsync, assignmentArgument, classroomOption);
  }
}