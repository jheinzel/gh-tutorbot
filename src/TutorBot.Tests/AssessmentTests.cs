using FluentAssertions;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Tests;

public class AssessmentTests
{
  const string correct1 = """
      # Erfüllungsgrad

      Aufwand (in Stunden): 10.5

      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1a        | 40          | 100               | 30               | 90           |
      | 1b        | 60          | 70                | 40               | 89.5         |
      """;

  const string correct2 = """
      Aufwand (in Stunden): 10.0
      | Beispiel  | Gewichtung  | Lösungsidee (15%) | Implement. (70%) | Testen (15%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 100          | 100              | 100               | 100         |
      """;

  const string correct3 = """
      # Erfüllungsgrad

      Aufwand (in Stunden): 0

      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 40          | 0                 | 0                | 0            |
      | 2         | 60          | 0                 | 0                | 0            |
      """;

  const string wrong1 = """
      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 40          | 0                 | 0                | 0            |
      """;

  const string wrong2 = """
      Aufwand (in Stunden): 10.0
      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | 
      | --------- | :---------: | :---------------: | :--------------: | 
      | 1         | 40          | 0                 | 0                | 
      """;

  const string wrong3 = """
      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (xxx%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 40          | 0                 | 0                | 0            |
      """;

  const string wrong4 = """
      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 40          | 0                 | xxx                | 0          |
      """;

  const string wrong5 = """
      | Beispiel  | Gewichtung  | Lösungsidee (20%) | Implement. (70%) | Testen (10%) |
      | --------- | :---------: | :---------------: | :--------------: | :----------: |
      | 1         | 4xxx0       | 0                 | xxx              | 0            |
      """;


  [Fact]
  public void Test_Assessment_Extensively()
  {
    Assessment assessment = new Assessment();
    assessment.LoadFromString(correct1);

    assessment.ColumnWeights.Count.Should().Be(3);
    assessment.ColumnWeights[0].Should().BeApproximately(20, 0.0001);
    assessment.ColumnWeights[1].Should().BeApproximately(70, 0.0001);
    assessment.ColumnWeights[2].Should().BeApproximately(10, 0.0001);

    assessment.Lines.Count.Should().Be(2);

    assessment.Lines[0].Exercise.Should().Be("1a");
    assessment.Lines[0].Weight.Should().BeApproximately(40, 0.0001);
    assessment.Lines[0].Gradings.Count.Should().Be(3);
    assessment.Lines[0].Gradings[0].Should().BeApproximately(100, 0.0001);
    assessment.Lines[0].Gradings[1].Should().BeApproximately(30, 0.0001);
    assessment.Lines[0].Gradings[2].Should().BeApproximately(90, 0.0001);

    assessment.Lines[1].Exercise.Should().Be("1b");
    assessment.Lines[1].Weight.Should().BeApproximately(60, 0.0001);
    assessment.Lines[1].Gradings.Count.Should().Be(3);
    assessment.Lines[1].Gradings[0].Should().BeApproximately(70, 0.0001);
    assessment.Lines[1].Gradings[1].Should().BeApproximately(40, 0.0001);
    assessment.Lines[1].Gradings[2].Should().BeApproximately(89.5, 0.0001);

    assessment.State.Should().Be(AssessmentState.Loaded);
    assessment.Effort.Should().BeApproximately(10.5, 0.01);
    assessment.Total.Should().BeApproximately(50.57, 0.01);
  }

  [Theory]
  [InlineData(correct1, 10.5, 50.57)]
  [InlineData(correct2, 10.0, 100)]
  [InlineData(correct3, 0.0, 0.0)]
  public void Assessment_ShouldBe_Correct(string content, double effort, double totalGrading)
  {
    var assessment = new Assessment();
    assessment.LoadFromString(content);

    assessment.ColumnWeights.Count.Should().Be(3);

    assessment.State.Should().Be(AssessmentState.Loaded);
    assessment.Effort.Should().BeApproximately(effort, 0.01);
    assessment.Total.Should().BeApproximately(totalGrading, 0.01);
  }

  [Theory]
  [InlineData(wrong1)]
  [InlineData(wrong2)]
  [InlineData(wrong3)]
  [InlineData(wrong4)]
  [InlineData(wrong5)]
  public void Assessment_ShouldBe_ThrowExcecption(string content)
  {
    var assessment = new Assessment();
    var action = () => assessment.LoadFromString(content);
    action.Should().Throw<AssessmentFormatException>();
  }
}

