using System.Net;
using System.Text;
using FluentAssertions;
using Octokit;
using TutorBot.Infrastructure.OctokitExtensions;

namespace TutorBot.Tests;

public class ReviewerAssignmentsClientTests
{
  private const string Org = "swo3";
  private const string Classroom = "classroom";
  private const string AssignmentSlug = "ue01";
  private const string FilePath = "classroom/reviewers/ue01.json";

  [Fact]
  public async Task Get_WhenFileDoesNotExist_ReturnsEmptyMapping()
  {
    var repositoryContents = Substitute.For<IRepositoryContentsClient>();
    repositoryContents.GetAllContents(Org, "classroom50", FilePath)
                      .Returns<Task<IReadOnlyList<RepositoryContent>>>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));
    var client = new ReviewerAssignmentsClient(repositoryContents);

    var result = await client.Get(Org, Classroom, AssignmentSlug);

    result.AssignmentSlug.Should().Be(AssignmentSlug);
    result.Repositories.Should().BeEmpty();
  }

  [Fact]
  public async Task Get_ReadsReviewerLoginsFromAssignmentFile()
  {
    var json = """
      {
        "schemaVersion": 1,
        "assignmentSlug": "ue01",
        "repositories": {
          "swo3/repo1": ["reviewer-one"]
        }
      }
      """;
    var repositoryContents = Substitute.For<IRepositoryContentsClient>();
    repositoryContents.GetAllContents(Org, "classroom50", FilePath)
                      .Returns(Task.FromResult<IReadOnlyList<RepositoryContent>>([CreateContent(json, "sha-1")]));
    var client = new ReviewerAssignmentsClient(repositoryContents);

    var result = await client.Get(Org, Classroom, AssignmentSlug);

    result.Repositories["SWO3/REPO1"].Should().ContainSingle().Which.Should().Be("reviewer-one");
  }

  [Fact]
  public async Task Save_WhenFileExists_UpdatesItUsingCurrentSha()
  {
    var repositoryContents = Substitute.For<IRepositoryContentsClient>();
    UpdateFileRequest? updateRequest = null;
    repositoryContents.GetAllContents(Org, "classroom50", FilePath)
                      .Returns(Task.FromResult<IReadOnlyList<RepositoryContent>>([CreateContent("{}", "sha-1")]));
    repositoryContents.UpdateFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdateFileRequest>())
                      .Returns(call =>
                      {
                        updateRequest = call.ArgAt<UpdateFileRequest>(3);
                        return Task.FromResult<RepositoryContentChangeSet>(null!);
                      });
    var client = new ReviewerAssignmentsClient(repositoryContents);
    var mapping = new ReviewerAssignmentsFile
    {
      AssignmentSlug = AssignmentSlug,
      Repositories = new(StringComparer.OrdinalIgnoreCase) { ["swo3/repo1"] = ["reviewer-one"] }
    };

    await client.Save(Org, Classroom, mapping);

    await repositoryContents.Received(1).UpdateFile(Org, "classroom50", FilePath, Arg.Any<UpdateFileRequest>());
    updateRequest.Should().NotBeNull();
    updateRequest!.Sha.Should().Be("sha-1");
    Encoding.UTF8.GetString(Convert.FromBase64String(updateRequest.Content)).Should().Contain("reviewer-one");
  }

  [Fact]
  public async Task Save_WhenFileDoesNotExist_CreatesIt()
  {
    var repositoryContents = Substitute.For<IRepositoryContentsClient>();
    CreateFileRequest? createRequest = null;
    repositoryContents.GetAllContents(Org, "classroom50", FilePath)
                      .Returns<Task<IReadOnlyList<RepositoryContent>>>(_ => throw new NotFoundException("Not found", HttpStatusCode.NotFound));
    repositoryContents.CreateFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CreateFileRequest>())
                      .Returns(call =>
                      {
                        createRequest = call.ArgAt<CreateFileRequest>(3);
                        return Task.FromResult<RepositoryContentChangeSet>(null!);
                      });
    var client = new ReviewerAssignmentsClient(repositoryContents);
    var mapping = new ReviewerAssignmentsFile { AssignmentSlug = AssignmentSlug };

    await client.Save(Org, Classroom, mapping);

    await repositoryContents.Received(1).CreateFile(Org, "classroom50", FilePath, Arg.Any<CreateFileRequest>());
    createRequest.Should().NotBeNull();
    Encoding.UTF8.GetString(Convert.FromBase64String(createRequest!.Content)).Should().Contain("assignmentSlug");
  }

  private static RepositoryContent CreateContent(string json, string sha)
  {
    var encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    return new RepositoryContent("ue01.json", FilePath, sha, json.Length, ContentType.File, "", "", "", "", "base64", encodedContent, "", "");
  }
}
