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

2. Install the TutorBot extension by executing the following command:
   ```shell
   gh extension install https://github.com/jheinzel/gh-tutorbot
   ```
3. If the TutorBot extension is already installed, update it to the recent
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
  gh tutorbot list-submissions <assignment> [--classroom <classroom>]
  ```

* Assign reviewers to an assignment randomly. Each submission will have one
  reviewer who gains read access to the submission repository and receives an
  invitation via email.
  ```shell
  gh tutorbot assign-reviewers <assignment> [--classroom <classroom>] [--force] [--dry-run]
  ```
  + `--force` allows reassignment of reviewers, even if some submissions are not
    yet linked.
  + `--dry-run` simulates executing the command without actually assigning
    reviewers.

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
  gh tutorbot clone-assignment <assignment> [--directory <directory>] [--classroom <classroom>]
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
  gh tutorbot list-review-statistics <assignment> [--classroom <classroom>] [--sort-by (reviewer|comment-length|review-date)]
  ```

* Perform a plagiarism check: Uses JPlag to cross-verify all assignment
  submissions for plagiarism. Before using this command, download the JPlag JAR
  file from https://github.com/jplag/JPlag/releases. Then install this JAR file
  in the 'lib' directory and ensure it's named 'jplag.jar'. Alternatively,
  adjust the configuration variable 'jplag-jar-path' as necessary. It is crucial
  to first clone the assignments using the `clone-assignment` command.

  The plagiarism check generates a ZIP file located in the assignment's
  directory. This ZIP file contains multiple JSON files that display the results
  of the plagiarism check. These results can be analyzed using the report viewer
  at [https://jplag.github.io/JPlag](https://jplag.github.io/JPlag). 
  ```shell
  gh tutorbot check-plagiarism <root-directory> [--language (cpp|java)] [--report-file <report-file>] [--refresh]
  ```
  + `root-directory` is the path of the directory containing all submissions.
  + `--language` specifies the programming language of the submissions.
  + `--report-file` specifies the base name of the report file.
  + `--refresh` forces a plagiarism check, even if the results file already
    exists.

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
    + After submitting your initial comment, select *Start a review* to provide
      additional comments.
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
4. Commit and