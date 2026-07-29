namespace TutorBot.Infrastructure.Exceptions;

public class OrganizationNotFoundException : InfrastrucureException
{
  public OrganizationNotFoundException(string org, string classroomName) 
    : base($"Organization '{org}' does not exist or the required '{Constants.CLASSROOM_METADATA_REPO_NAME}' repository is not accessible. Attempted to access classroom '{classroomName}'.")
  {
  }
}
