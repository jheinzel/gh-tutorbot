using System.Runtime.CompilerServices;
using Octokit;
using TutorBot.Infrastructure;
using TutorBot.Infrastructure.OctokitExtensions;
using TutorBot.Infrastructure.StringExtensions;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

public class Assignment
{
  public required string Name { get; init; }
  public DateTime? Deadline { get; init; }

  public IEnumerable<Submission> Submissions { get; init; } = Enumerable.Empty<Submission>();

  public static async Task<Assignment> FromGitHub(IGitHubClient client, StudentList students, long classroomId, string assignmentName)
  {
    var submissions = new List<Submission>();

    var assignmentDto = await client.Classroom().Assignment.GetByName(classroomId, assignmentName);
    var repositories = await client.Classroom().Submissions.GetAll(assignmentDto.Id);

    foreach (var repo in repositories)
    {
      var collaborators = (await client.Repository.Collaborator.GetAll(repo.Id))
                            .Where(c => c.Permissions.Maintain is not null && c.Permissions.Maintain == false)
                            .ToList();

      var collaboratorsWithWriteAccess = collaborators.Where(c => c.RoleName == Constants.GITHUB_WRITE_ROLE).ToList();
      if (collaboratorsWithWriteAccess.Count == 0)
      {
        throw new RepositoryException($"No collaborator with write permissions assigned to repository \"{repo.Name}\".");
      }
      if (collaboratorsWithWriteAccess.Count > 1)
      {
        throw new RepositoryException($"More than one collaborator with write permissions assigned to repository \"{repo.Name}\".");
      }
      if (!students.TryGetValue(collaboratorsWithWriteAccess[0].Login, out var owner))
      {
        throw new RepositoryException($"No student assigned to GitHub user \"{collaboratorsWithWriteAccess[0].Login}\".");
      }


      var reviewers = new List<Student>();
      foreach (var collaborator in collaborators.Where(c => c.RoleName == Constants.GITHUB_READ_ROLE))
      {
        if (students.TryGetValue(collaborator.Login, out var reviewer))
        {
          reviewers.Add(reviewer);
        }
        else
        {
          throw new RepositoryException($"No student assigned to reviewer \"{collaborator.Login}\".");
        }
      }

      submissions.Add(new Submission { RepositoryName = repo.Name, RepositoryUrl = repo.HtmlUrl, Student = owner, Reviewers = reviewers });
    }

    return new Assignment { Name = assignmentDto.Title, Deadline = assignmentDto.Deadline?.ToDateTime(), Submissions = submissions };
  }
}
