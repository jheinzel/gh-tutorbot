using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Logic;


public class ReviewStatisticsItem
{
  public int NumReviews { get; set; }
  public int NumComments { get; set; }
  public int NumWords { get; set; }
  public DateTimeOffset LastReviewDate { get; set; }
}
