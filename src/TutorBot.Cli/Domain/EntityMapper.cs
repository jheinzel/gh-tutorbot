using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TutorBot.Domain;

public class EntityMapper<T> where T : notnull
{
  private readonly IList<T> entities;
  private readonly IDictionary<T, T> givenMappings;
  private readonly IDictionary<T, int> orderId;

  private IEnumerable<T> RotatedList(List<T> list, int first)
  {
    if (first < 0 || first >= list.Count)
    {
      throw new IndexOutOfRangeException($"frist out of range [0, {list.Count}");
    }

    for (int i = 0, index = first; i < list.Count; i++, index = (index + 1) % list.Count)
    {
      yield return list[index];
    }
  }

  private int FindNextIndexModular(List<T> entities, T start)
  {
    int first = entities.FindIndex(t => orderId[t] > orderId[start]);
    if (first == -1)
    {
      first = entities.FindIndex(t => orderId[t] < orderId[start]);
    }

    return first;
  }

  public EntityMapper(IEnumerable<T> entities, IEnumerable<(T, T)> givenMappings)
  {
    this.entities = entities.ToList();

    this.givenMappings = new Dictionary<T, T>();
    var m = givenMappings.ToList();
    foreach (var (source, target) in givenMappings)
    {
      this.givenMappings.Add(source, target);
    }

    this.orderId = new Dictionary<T, int>(this.entities.Count);
    for (int i = 0; i < this.entities.Count; i++)
    {
      orderId.Add(this.entities[i], i);
    }
  }


  public IDictionary<T, T> FindUniqueMapping()
  {
    var mapping = new Dictionary<T, T>();

    var unmappedEntities = entities.Where(e => !givenMappings.ContainsKey(e)).ToList();

    var untargetedEntitySet = new HashSet<T>(entities);
    untargetedEntitySet.RemoveWhere(givenMappings.Values.Contains);
    var untargetedEntities = entities.Where(e => untargetedEntitySet.Contains(e)).ToList();

    if (unmappedEntities.Count != untargetedEntities.Count)
    {
      throw new InvalidOperationException("Unexpected internal state: Number of sources must be equal to number of targets");
    }

    if (unmappedEntities.Count == 0 ||
        (unmappedEntities.Count == 1 && unmappedEntities[0].Equals(untargetedEntities[0])))
    {
      return mapping;
    }

    int first = FindNextIndexModular(untargetedEntities, unmappedEntities[0]);
    var targets = RotatedList(untargetedEntities, first).GetEnumerator();

    foreach (var source in unmappedEntities)
    {
      targets.MoveNext();
      mapping.Add(source, targets.Current);
    }

    return mapping;
  }
}
