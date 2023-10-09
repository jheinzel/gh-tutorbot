using FluentAssertions;
using TutorBot.Domain;

namespace TutorBot.Tests;

public static class CollectionExtensions
{
  public static void DeterministicShuffle<T>(this IList<T> list, int seed)
  {
    var random = new Random(seed);

    for (int i = list.Count - 1; i > 0; i--)
    {
      int k = random.Next(i);
      (list[k], list[i]) = (list[i], list[k]);
    }
  }
}

public class EntityMapperTests
{
  private Func<string, string, bool> suffixEqual = (s, t) => !s[1..].Equals(t[1..]);

  private static IEnumerable<string> CreateStrings(string prefix, int num)
  {
    for (int i = 0; i < num; i++)
    {
      yield return $"{prefix}{i}";
    }
  }

  private static void MappingShouldBeCorrect<T>(IList<T> entities, IDictionary<T, T> mapping)
  {
    mapping.Should().HaveCount(entities.Count);
    foreach (var (s, t) in mapping)
    {
      t.Should().NotBeNull();
      s.Should().NotBe(t);
    }
    mapping.Values.Should().OnlyHaveUniqueItems();
    mapping.Keys.Should().OnlyContain(e => entities.Contains(e));
    mapping.Values.Should().OnlyContain(e => entities.Contains(e));
  }

  [Fact]
  public void Mapping_WithoutEntities_ShouldHaveEmpyMapping()
  {
    var entities = new List<string>();
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().BeEmpty();
  }

  [Fact]
  public void Mapping_WithOneEntity_ShouldHaveEmpyMapping()
  {
    var entities = new List<string> { "E1" };
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().BeEmpty();
  }


  [Fact]
  public void Mapping_WithTwoEntities_ShouldBeCorrect()
  {
    var entities = new List<string> { "E1", "E2" };
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().HaveCount(2);
    mapping["E1"].Should().BeEquivalentTo("E2");
    mapping["E2"].Should().BeEquivalentTo("E1");
  }

  [Fact]
  public void Mapping_WithTwoEntitiesInReverseOrder_ShouldBeCorrect()
  {
    var entities = new List<string> { "E2", "E1" };
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().HaveCount(2);
    mapping["E1"].Should().BeEquivalentTo("E2");
    mapping["E2"].Should().BeEquivalentTo("E1");
  }


  [Theory]
  [InlineData(100)]
  public void Mapping_WithoutGivenMappings_ShouldBeCorrect(int n)
  {
    var entities = CreateStrings("E", n);
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    MappingShouldBeCorrect(entities.ToList(), mapping);
  }

  [Fact]
  public void Mapping_WithTreeEntitiesAndOneGivenMapping_ShouldBeCorrect()
  {
    var entities = new List<string> { "E0", "E1", "E2" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E1"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void Mapping_WithTreeEntitiesAndTwoGivenMappings_ShouldBeCorrect()
  {
    var entities = new List<string> { "E0", "E1", "E2" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E1"),
      ("E1", "E2"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void Mapping_StressTest()
  {
    for (int nEntities = 5; nEntities <= 8; nEntities++)
    {
      for (var i = 0; i < 10; i++)
      {
        var entities = CreateStrings("E", nEntities).ToList();
        var givenMapping = new List<(string, string)>
        {
          ("E1", "E0"),
          ("E0", "E2"),
          ("E3", "E4")
        };
        entities.DeterministicShuffle(i * 10);

        var mapper = new EntityMapper<string>(entities, givenMapping);

        var newMapping = mapper.FindUniqueMapping();

        var resultingMapping = new Dictionary<string, string>();
        foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
        foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

        MappingShouldBeCorrect(entities.ToList(), resultingMapping);
      }
    }
  }

  [Fact]
  public void Mapping_WithGivenCycle_ShouldBeCorrect()
  {
    var entities = new List<string> { "E0", "E1", "E2", "E3" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E1"),
      ("E1", "E0"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void Mapping_WithGivenCycleAndIsolatedNode_ReturnsEmptyMapping()
  {
    var entities = new List<string> { "E0", "E1", "E2" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E1"),
      ("E1", "E0"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    newMapping.Should().BeEmpty();
  }

  [Fact]
  public void Mapping_WithFourEntitiesAndOneGivenMapping_ShouldBeCorrect_()
  {
    var entities = new List<string> { "E0", "E1", "E2", "E3" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E2"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void Mapping_WithGivenMappingAndTwoHoles_ShouldBeCorrect()
  {
    var entities = CreateStrings("E", 8);
    var givenMapping = new List<(string, string)>
    {
      ("E1", "E2"),
      ("E2", "E3"),
      ("E7", "E6"),
      ("E6", "E5"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Theory]
  [InlineData(3)]
  [InlineData(10)]
  [InlineData(30)]
  public void Mapping_WithGivenMappings_ShouldBeCorrect(int n)
  {
    var entities = CreateStrings("E", n).ToList();
    var givenMapping = new List<(string, string)>();
    for (int i = 0; i < n; i++)
    {
      if (i % 3 == 1)
      {
        givenMapping.Add((entities[i], entities[(i + 1) % n]));
      }
    }

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }
}
