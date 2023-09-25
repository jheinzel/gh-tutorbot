using FluentAssertions;
using TutorBot.Infrastructure.CollectionExtensions;
using TutorBot.Utility;

namespace TutorBot.Tests;

public class ListExtensionTests
{
  [Fact]
  public void ShuffledOneElements_ShouldBeUnchnaged()
  {

    var list = new List<int> { 1 };
    list.Shuffle();

    list.Should().Contain(1);
  }

  [Fact]
  public void ShuffledTwoElementsList_ShouldBeAPermutation()
  {

    var list = new List<int> { 1, 2 };
    list.Shuffle();

    list.Should().Contain(list);
  }

  [Fact]
  public void ShuffledList_ShouldBeAPermutation()
  {

    var list = new List<int> { 1, 2, 3, 4, 5 };
    list.Shuffle();

    list.Should().Contain(list);
  }
}

