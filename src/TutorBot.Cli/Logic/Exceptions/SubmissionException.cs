using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Logic.Exceptions;

public class SubmissionException : LogicException
{
  public SubmissionException(string message) : base(message)
  {
  }
}
