namespace TutorBot.Domain;


public class ReviewStatisticsItem
{
  public int NumReviews { get; set; }
  public int NumComments { get; set; }
  public int NumWords { get; set; }
  public DateTimeOffset? LastReviewDate { get; set; }
}
