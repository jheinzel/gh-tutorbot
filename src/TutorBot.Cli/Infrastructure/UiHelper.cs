namespace TutorBot.Infrastructure;

public static class UiHelper
{
  public static string GetUserInput(string prompt, string[] answerOptions, string? defaultAnswer = null)
  {
    string? answer = null;
    var lowerCaseAnswers = answerOptions.Select(a => a.ToLower());
    while (answer is null)
    {
      Console.Write(prompt);

      string? userInput = Console.ReadLine();
      if (!string.IsNullOrEmpty(userInput) && lowerCaseAnswers.Contains(userInput.ToLower()))
      {
        answer = userInput;
      }
      else if (string.IsNullOrEmpty(userInput) && defaultAnswer is not null)
      {
        answer = defaultAnswer;
      }
    }

    return answer.ToLower();
  }
}
