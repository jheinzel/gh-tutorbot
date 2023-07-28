using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using TutorBot.Logic.Exceptions;

namespace TutorBot.Logic;

public class Assignment
{
  public required string Name { get; init; }
  public required string Organization { get; init; }

  public IEnumerable<Submission> Submissions { get; init; } = Enumerable.Empty<Submission>();

  public static async Task<Assignment> FromGitHub(IGitHubClient client, StudentList students, string organization, string name)
  {
    var submissions = new List<Submission>();

    var repositories = await client.Repository.GetAllForOrg(organization);
    foreach (var repo in repositories)
    {
      if (repo.Name.StartsWith(name))
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
        if (! students.TryGetValue(collaboratorsWithWriteAccess[0].Login, out var owner))
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

        submissions.Add(new Submission { RepositoryName = repo.Name, Student = owner, Reviewers = reviewers });
      }
    }

    return new Assignment { Name = name, Organization = organization, Submissions = submissions };
  }
}
