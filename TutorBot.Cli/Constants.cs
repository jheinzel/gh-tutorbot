namespace TutorBot;

internal static class Constants
{
  public const string APP_NAME = "gh-tutorbot";
  public const string ROSTER_FILE_PATH = @"./roster.csv";
  public const string STUDENT_DATA_PATTERN = @"^(?<LastName>[A-Za-zäöüß]+)\s+(?<FirstName>[A-Za-zäöüß\.\-\s]+)\s*\(G(?<GroupNr>\d)/(?<MatNr>S\d+)\)$";
  // "LastName FirstName1 FirstName2 ... (G9/S999999999)";
  public const string GITHUB_READ_ROLE = "read";
  public const string GITHUB_WRITE_ROLE = "write";
}
