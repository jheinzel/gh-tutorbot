using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Logic.Exceptions;

public class RepositoryException : LogicException
{
  public RepositoryException(string message) : base(message)
  {
  }
}
