# GitHub TutorBot

*GitHub TutorBot* is a straightforward command-line tool designed to assist
programming lecturers and tutors at the University of Applied Sciences in
Hagenberg by automating repetitive tasks.

## Installation

1. TutorBot operates as an extension for the [GitHub
   CLI](https://cli.github.com/). Please install it before proceeding with the
   installation of TutorBot. Follow the instructions that can be found
   [here](https://github.com/cli/cli#installation). Prepackaged binaries are
   also available [here](https://github.com/cli/cli/releases).

2. Log in to GitHub using the GitHub CLI:
   ```sh
   gh auth login
   ```
   This generates a security token that is used by TutorBot to access GitHub.
   The authentication status can be verified with `gh auth status`.

3. Install the TutorBot extension by executing the following command:
   ```shell
   gh extension install https://github.com/jheinzel/gh-tutorbot
   ```
4. If the TutorBot extension is already installed, update it to the recent
   version using this command:
   ```shell
   gh extension upgrade tutorbot
   ```

## Commands

TutorBot offers the following range of commands:

* List all students (extracted from the roster file - `classroom_roster.csv`):
  ```shell
  gh tutorbot list-students
  ```

* List all classrooms the user is a member of:
  ```shell
  gh tutorbot list-classrooms
  ```

* List all assignments created in a specific classroom:
  ```shell
  gh tutorbot list-assignments [--classroom <classroom>]
  ```

* List all submissions for a specific assignment:
  ```shell
      gh tutorbot list-submissions <assignment> [--classroom <classroom>] [--group <nr>]
  ```
  + `--group`: Filter by group. The group number is specified as a positive
    integer. If omitted, all groups are considered.

* Assign reviewers to an assignment randomly. Each submission will have one
  reviewer who gains read access to the submission repository and receives an
  invitation via email. If there are already reveiwers assigned, the command
  will preserve these assignments and add add the missing ones.
  ```shell
  gh tutorbot assign-reviewers <assignment> [--classroom <classroom>] [--force]
  ```
  + `--force` allows assigment of reviewers, even if some submissions are not
    yet linked.

* Remove reviewers from an assignment: 
  ```shell
  gh tutorbot remove-reviewers <assignment> [--classroom <classroom>]
  ```

* Clone all repositories of a specific assignment: The target directory for
  cloned repositories can be specified. If omitted, the current working
  directory is used. The directory is created if it does not exist. However, if
  the directory is not empty, the command will fail. This command delegates to
  `gh repo clone`. In case of problems check if this command works correctly. 
  ```shell
  gh tutorbot clone-submissions <assignment> [--directory <directory>] [--classroom <classroom>]
  ```

* Download students' self-assessments: Collects self-assessment data from all
  submissions and writes it to a CSV file, named `<assignment>-assessments.csv`,
  placed in the current working directory.
  ```shell
  gh tutorbot download-assessments <assignment> [--classroom <classroom>]
  ```

* List review statistics: Provides statistical data about the activity of the
  reviewers.
  ```shell
  gh tutorbot list-review-statistics <assignment>
     [--classroom <classroom>] 
     [--order-by (reviewer|comment-length[-desc]|review-date[-desc])] 
     [--group <nr>]
     [--all-reviewers]
  ```
  + `--order-by` specifies how the statistics data is ordered:
    + `reviewer`: Order by reviewer name.
    + `comment-length`: Order by comment length (ascending).
    + `comment-length-desc`: Order by comment length (descending).
    + `review-date`: Order by last review date (ascending).
    + `review-date-desc`: Order by last review date (descending).
  + `--group`: Filter by group. The group number is specified as a positive
    integer. If omitted, all groups are considered
  + `--all-reviewers`: Show statistics from all reviewers: students, letchers, 
    and tutors. If omitted, only students are considered.

* Perform a plagiarism check: Uses JPlag to cross-verify all assignment
  submissions for plagiarism. Before using this command, download the JPlag JAR
  file from https://github.com/jplag/JPlag/releases. Then install this JAR file
  in the 'lib' directory and ensure it's named 'jplag.jar'. Alternatively,
  adjust the configuration variable 'jplag-jar-path' as necessary. It is crucial
  to first clone the assignments using the `clone-assignment` command.

  The plagiarism check generates a .jplag file (in ZIP format) located in the assignment's
  directory. This file contains multiple JSON files that display the results
  of the plagiarism check. These results can be analyzed using the `view-plagiarism-report` command. 
  ```shell
  gh tutorbot check-plagiarism <root-directory> [--language (cpp|java|c)] [--report-file <report-file>] [--refresh] [--base-code <base-code-directory>]
  ```
  + `root-directory` is the path of the directory containing all submissions.
  + `--language` specifies the programming language of the submissions.
  + `--report-file` specifies the base name of the report file.
  + `--refresh` forces a plagiarism check, even if the results file already
    exists.
  + `--base-code` is the path to the directory containing the template of the submission. It can be used to ignore already provided code from being recognized as plagiarism

* View plagiarism report: Launches a local web server to display the results of
  a plagiarism check in your browser. This command opens the JPlag report
  viewer, providing an interactive interface to explore detected similarities
  between submissions, review comparison details, and analyze matched code
  segments.
  ```shell
  gh tutorbot view-plagiarism-report [--report-file <report-file>]
  ```
  + `--report-file` specifies the base name of the report file [Default:
    `./plagiarism-report.jplag`].

* To get help:
  ```shell
  gh tutorbot [<command>] --help
  ```

## Configuration
* `classroom_roster.csv`: This roster file containing a list of students in a
  classroom is used primarily to map the GitHub username to the student's ID
  (matriculation number) and name. The roster file can be downloaded from the
  classroom page on GitHub as follows: Classroom page → Students → Download.
  Place this file in your working directory and name it as
  `classroom_roster.csv`.

* `appsettings.json`: This configuration file contains general settings for the
  .NET application along with specific settings for TutorBot.
  ```json
  {
    "default-classroom": "my-classroom",
    "java-path": "java",
    "jplag-jar-path": "lib/jplag.jar"
  }
  ```
  + `default-classroom`: The default value for the `--classroom` option.
  + `java-path`: The path to the Java executable.
  + `jplag-jar-path`: The path to the JPlag JAR file (absolute path or relative
    to the working directory). 
  
## Working with TutorBot

1. Verify if you are a member with admin permissions in the classroom you are
   working with. If not, ask the classroom owner to add you as a member with the
   required permissions.
2. Install GitHub CLI and TutorBot as described above.
3. Download the student roster file to your working directory. Create the
   `appsettings.json` configuration file and define your default classroom.
4. Authenticate with GitHub CLI:
   ```shell
   gh auth login
   ```
   You can verify your authentication status with:
   ```shell
   gh auth status
   ```
   TutorBot will utilize the generated security token to access the GitHub API.
5. Apply the commands above to your classroom as needed.

## Feedback Submission

Lecturers, tutors, and students can submit their feedback by adhering to the
following instructions:
* Access the submitted project's repository in a web browser.
* Navigate to the *Pull requests* tab and select *Feedback*
  (`<repository-url>/pulls/1`).
* On the *Feedback* page, click on *Files changed*.
* Proceed to add comments related to the code and the documentation.
    + Choose specific lines or the entire file for providing feedback. 
    + After submitting your initial comment, select *Start a review* 
    + Add additional comments (*Add review comments*).
    + Finalize your review by expressing a general comment for the complete
      review, and click on *Submit review*.
    + Multiple reviews can be submitted to a single pull request.
* Please refer to [Commenting on a Pull
  Request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/commenting-on-a-pull-request)
  in the GitHub documentation for more in-depth information.

## Standard Process for Lecturers

1. Start the process by creating a new assignment in the classroom with these
   preferences:
   * Individual assignment.
   * Private repository.
   * Ensure students do not have admin permissions on their repositories.
   * Optionally, include a starter code repository.
   * Essential: Enable feedback pull requests.
2. Share the invitation link with the students (for instance, via Moodle).
3. Once the assignment deadline has passed, delegate review assignments to
   students using the `gh tutorbot assign-reviewers` command.
4. Download the student's self-assessment files using the `gh tutorbot
   download-assessments` command.
5. Proceed with providing your feedback.

## Standard Process for Tutors

1. Carry out a plagiarism check using `gh tutorbot check-plagiarism`. Ensure all
   submissions have been first downloaded using the `gh tutorbot
   clone-assignment` command.
2. Provide feedback on students' submissions and their corresponding reviews.
3. Share feedback with the lecturers regarding commonly made mistakes in the
   students' solutions.

## Standard Process for Students

1. Make a copy of the assignment repository by following the invitation link.
2. Complete the assignment and prepare the necessary documentation.
3. Fill in the self-assessment table in the `ASSESSMENT.md` file.
4. Commit and push your changes to the repository.
5. Review the submission of the student you have been assigned to.