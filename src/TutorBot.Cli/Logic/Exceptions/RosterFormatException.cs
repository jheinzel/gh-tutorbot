using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Logic.Exceptions;

public class RosterFormatException : LogicException
{
  public RosterFormatException(string message) : base(message)
  {
  }
}
