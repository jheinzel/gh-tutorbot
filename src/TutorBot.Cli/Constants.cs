namespace TutorBot;

internal static class Constants
{
  public const string APP_NAME = "gh-tutorbot";

  public const string DOUBLE_PATTERN = @"(?<number>[+-]?(\d*[.])?\d+)";
  public const string ROSTER_FILE_PATH = @"./classroom_roster.csv";
  public const string STUDENT_DATA_PATTERN = @"^(?<LastName>[\w\.\-]+)\s+(?<FirstName>([\w\.\-\s]*[\w\.\-]))\s*\(G(?<GroupNr>\d)/(?<MatNr>S\d+)\)$";
    // "LastName FirstName1 FirstName2 ... (G9/S999999999)";
  public const string ASSESSMENT_HEADER_ENTRY_PATTERN = $@"\(\s*(?<Value>{DOUBLE_PATTERN})\s*\%\s*\)";
    // "(99.9%)", " xxx (99.9%) xxx", "(99.9 % )", "( 99.9 %)", ...

  public const string GITHUB_READ_ROLE = "read";
  public const string GITHUB_WRITE_ROLE = "write";

  public const string PAGE_KEY = "page";
  public const string PER_PAGE_KEY = "per_page";
  public const int SUBMISSIONS_PAGE_SIZE = 5;

  public const string ASSESSMENT_FILE_NAME = "ASSESSMENT.md";
  public const string ASSESSMENTS_DOWNLOAD_FILE_NAME = "{0}-assessments.csv"; // {0} = assignment name
  public const string EFFORT_PREFIX = "Aufwand"; // {0} = assignment name
  public const int FEEDBACK_PULLREQUEST_ID = 1;

  public const string JPLAG_ARGS = "-l {0} -r \"{1}\" \"{2}\""; // {0} = language, {1} = report file, {2} = root directory
  public const string DEFAULT_REPORT_FILE = "plagiarism-report";
}
