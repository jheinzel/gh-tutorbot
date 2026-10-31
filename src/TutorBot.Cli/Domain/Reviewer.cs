namespace TutorBot.Domain;

public class Reviewer(Student student) : Student(student.GitHubUsername, student.LastName, student.FirstName, student.MatNr, student.GroupNr)
{
}
