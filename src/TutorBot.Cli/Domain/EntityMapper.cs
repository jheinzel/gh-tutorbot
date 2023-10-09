using TutorBot.Domain.Exceptions;

namespace TutorBot.Domain;

public class EntityMapper<T> where T : notnull
{
  private readonly IList<T> entities;
  private readonly IDictionary<T, T> givenMapping;


  public EntityMapper(IEnumerable<T> entities, IEnumerable<(T, T)> givenMappings)
  {
    this.entities = entities.ToList();

    this.givenMapping = new Dictionary<T, T>();
    var m = givenMappings.ToList();
    foreach (var (source, target) in givenMappings)
    {
      this.givenMapping.Add(source, target);
    }

    CheckIfMappingIsValid(givenMapping);
  }

  public IDictionary<T, T> FindUniqueMapping()
  {
    if (givenMapping.Count == 0)
    {
      return FindUniqueMappingWithoutPredefinitions();
    }
    else
    {
      return FindUniqueMappingWithPredefinitions();
    }
  }

  private T FindEndOfPath(IDictionary<T, T> mapping, T start)
  {
    var current = start;
    while (mapping.TryGetValue(current, out var next))
    {
      current = next;
    }

    return current;
  }

  private bool IsCycle(IDictionary<T, T> mapping, T source, T target)
  {
    var unionMapping = new Dictionary<T, T>();
    foreach (var (s, t) in givenMapping)
    {
      unionMapping.Add(s, t);
    }
    foreach (var (s, t) in mapping)
    {
      unionMapping.Add(s, t);
    }
    unionMapping.Add(source, target);

    var current = source;
    while (unionMapping.TryGetValue(current, out var next))
    {
      if (next.Equals(source))
      {
        return true;
      }
      current = next;
    }
    return false;
  }

  private void CheckIfMappingIsValid(IDictionary<T, T> mapping)
  {
    var inDegree = new Dictionary<T, int>();
    foreach (var (_, target) in mapping)
    {
      inDegree[target] = inDegree.GetValueOrDefault(target) + 1;
    }

    var nonUniqueTargets = new Dictionary<T, int>();
    var notUniqueTargets = inDegree.Where(kvp => kvp.Value > 1).Select(kvp => kvp.Key).ToList();
    if (notUniqueTargets.Count > 0)
    {
      throw new NonUniqueValuesException<T>("EntityMapper was passed an invalid mapping.",
                                                   notUniqueTargets);
    }
  }

  private IDictionary<T, T> FindUniqueMappingWithoutPredefinitions()
  {
    var mapping = new Dictionary<T, T>();
    if (entities.Count <= 1)
    {
      return mapping;
    }

    for (var i = 0; i < entities.Count; i++)
    {
      var j = (i + 1) % entities.Count;
      mapping.Add(entities[i], entities[j]);
    }

    return mapping;
  }

  private IDictionary<T, T> FindUniqueMappingWithPredefinitions()
  {
    var mapping = new Dictionary<T, T>();

    var unmappedEntities = entities.Where(e => !givenMapping.ContainsKey(e)).ToList();
    if (unmappedEntities.Count == 0)
    {
      return mapping;
    }

    var untargetedEntities = new HashSet<T>(entities);
    untargetedEntities.RemoveWhere(givenMapping.Values.Contains);

    var start = untargetedEntities.First();
    untargetedEntities.Remove(start);
    var current = FindEndOfPath(givenMapping, start);

    while (untargetedEntities.Count > 0)
    {
      var successor = untargetedEntities.First();

      foreach (var target in untargetedEntities)
      {
        if (!IsCycle(mapping, current, target))
        {
          successor = target;
          break;
        }
      }

      untargetedEntities.Remove(successor);
      mapping.Add(current, successor);
      current = FindEndOfPath(givenMapping, successor);
    }

    if (!current.Equals(start))
    {
      mapping.Add(current, start); // close the cycle
    }

    return mapping;
  }
}
