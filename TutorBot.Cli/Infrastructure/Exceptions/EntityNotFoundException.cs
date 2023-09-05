using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TutorBot.Infrastructure.Exceptions;

namespace TutorBot.Logic.Exceptions;

public class EntityNotFoundException : InfrastrucureException
{
  public EntityNotFoundException(Type type, string name) : base($"{type.Name} with name \"{name}\" not found")
  {
  }
}
