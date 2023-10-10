using FluentAssertions;
using TutorBot.Domain;
using TutorBot.Domain.Exceptions;

namespace TutorBot.Tests;

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
  public void FindMapping_WithoutEntities_ShouldHaveEmpyMapping()
  {
    var entities = new List<string>();
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().BeEmpty();
  }

  [Fact]
  public void FindFindMapping_WithOneEntity_ShouldReturnEmptyMapping()
  {
    var entities = new List<string> { "E1" };
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    mapping.Should().BeEmpty();
  }


  [Fact]
  public void FindFindMapping_WithTwoEntities_ShouldReturnCorrectMapping()
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
  public void FindFindMapping_WithTwoEntitiesInReverseOrder_ShouldReturnCorrectMapping()
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
  public void FindMapping_WithoutGivenFindMapping_ShouldReturnCorrectMapping(int n)
  {
    var entities = CreateStrings("E", n);
    var noGivenMappings = new List<(string, string)>();
    var mapper = new EntityMapper<string>(entities, noGivenMappings);

    var mapping = mapper.FindUniqueMapping();

    MappingShouldBeCorrect(entities.ToList(), mapping);
  }

  [Fact]
  public void FindMapping_WithTreeEntitiesAndOneGivenFindMapping_ShouldReturnCorrectMapping()
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
  public void FindMapping_WithTreeEntitiesAndTwoGivenMappings_ShouldReturnCorrectMapping()
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
  public void FindMapping_WithForEntitesAndTwoGivenMappings_ShouldReturnCorrectMapping()
  {
    var entities = new List<string> { "E0", "E1", "E2", "E3" };
    var givenMapping = new List<(string, string)>
    {
      ("E1", "E0"),
      ("E2", "E3"),
    };

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void FindMapping_WithForEntitesAndOneGivenFindMapping_ShouldReturnCorrectMapping()
  {
    var entities = new List<string> { "E0", "E1", "E2", "E3" };
    var givenMapping = new List<(string, string)>
    {
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
  public void FindMapping_WithGivenCycle_ShouldReturnCorrectMapping()
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
  public void FindMapping_WithGivenCycleAndIsolatedNode_ReturnsEmptyMapping()
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
  public void FindMapping_WithFourEntitiesAndOneGivenFindMapping_ShouldReturnCorrectMapping_()
  {
    var entities = new List<string> { "E0", "E1", "E2", "E3" };
    var givenMapping = new List<(string, string)>
    {
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
  public void FindMapping_WithGivenMappingAndTwoHoles_ShouldReturnCorrectMapping()
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


  [Fact]
  public void FindMapping_WithComplexGivenFindMapping_ShouldReturnCorrectMapping()
  {
    int nEntities = 30;

    var entities = CreateStrings("E", nEntities).ToList();
    var givenMapping = new List<(string, string)>();
    int i = 0;
    bool chainForward = true;

    while (i < nEntities)
    {
      for (int j = 0; i < nEntities && j < 2; i++, j++)
      {
        if (chainForward)
        {
          givenMapping.Add((entities[i], entities[(i + 1) % nEntities]));
        }
        else // chainBackward
        {
          givenMapping.Add((entities[i], entities[(i - 1 + nEntities) % nEntities]));
        }
      }

      chainForward = !chainForward;
      i += 2;
    }

    var mapper = new EntityMapper<string>(entities, givenMapping);

    var newMapping = mapper.FindUniqueMapping();

    var resultingMapping = new Dictionary<string, string>();
    foreach (var (s, t) in givenMapping) resultingMapping.Add(s, t);
    foreach (var (s, t) in newMapping) resultingMapping.Add(s, t);

    MappingShouldBeCorrect(entities.ToList(), resultingMapping);
  }

  [Fact]
  public void FindMapping_WithNonUniqueTargets_ShouldThrowException()
  {
    var entities = new List<string> { "E0", "E1", "E2" };
    var givenMapping = new List<(string, string)>
    {
      ("E0", "E2"),
      ("E1", "E2")
    };

    var findUniquMappingAction = () => new EntityMapper<string>(entities, givenMapping);
    findUniquMappingAction.Should()
      .Throw<NonUniqueValuesException<string>>()
        .Which.NonUniqueValues.Should()
              .HaveCount(1).And
              .Contain("E2");
  }
}