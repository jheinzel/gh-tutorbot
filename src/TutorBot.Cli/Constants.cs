namespace TutorBot;

internal static class Constants
{
  public const string APP_NAME = "gh-tutorbot";
  public const string ROSTER_FILE_PATH = @"./classroom_roster.csv";
  public const string STUDENT_DATA_PATTERN = @"^(?<LastName>[A-ZÄÖÜa-zäöüß\.\-]+)\s+(?<FirstName>([A-ZÄÖÜa-zäöüß\.\-\s]*[A-ZÄÖÜa-zäöüß\.\-]))\s*\(G(?<GroupNr>\d)/(?<MatNr>S\d+)\)$";
  // "LastName FirstName1 FirstName2 ... (G9/S999999999)";
  public const string GITHUB_READ_ROLE = "read";
  public const string GITHUB_WRITE_ROLE = "write";
  public const string ASSESSMENT_FILE_NAME = "ASSESSMENT.md";
  public const string ASSESSMENTS_DOWNLOAD_FILE_NAME = "{0}-assessments.csv"; // {0} = assignment name
  public const int FEEDBACK_PULLREQUEST_ID = 1;
}
