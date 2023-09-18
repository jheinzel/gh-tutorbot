# GitHub TutorBot

*GitHub TutorBot* is a simple command line tool that helps programming tutors at the University of Applied Sciences in Hagenberg
by automating repetitive tasks.


## Installation

1. TutorBot is an extension for the [GitHub CLI](https://cli.github.com/), so you need to install it first. Installation instructions
   can be found [here](https://github.com/cli/cli#installation). Prepackaged binaries can be found [here](https://github.com/cli/cli/releases/).

2. Install the TutorBot extension by running the following command:
   ```shell
   gh extension install https://github.com/jheinzel/gh-tutorbot
   ```

## Commands

TutorBot comes with the following range of commands:

* List all classrooms the user is a member of:
  ```shell
  gh tutorbot list-classrooms
  ```

  * List all assignments created in a classroom :
  ```shell
  gh tutorbot list-assignments [--classroom <classroom>]
  ```

* List all submissions of an assignment:
  ```shell
  gh tutorbot list-submissions <assignment> [--classroom <classroom>]
  ```

* Assign reviewers to an assignment: One reviewer per submission is assigned in a random fashion.
  The reviewer gets read access to the submission repository and gets an invitation via email.
  ```shell
  gh tutorbot assign-reviewers <assignment> [--classroom <classroom>]
  ```

* Assign reviewers from an assignment: 
  ```shell
  gh tutorbot remove-reviewers <assignment> [--classroom <classroom>]
  ```

* Clone all repositories of an assignment: The directory the repositories are cloned to can be specified.
  If omitted, the current working directory is used. The directory is created if it does not exist.
  If the directory ist not empty, the command will fail.
  ```shell
  gh tutorbot clone-assignment <assignment> [--directory <directory>] [--classroom <classroom>]
  ```

* Download student's self assessments: Collect self assessment data from all submissions and write it
  to a CSV file. The CSV file is named `<assignment>-assessments.csv` and placed in the current working directory.
  ```shell
  gh tutorbot download-assessments <assignment> [--classroom <classroom>]
  ```

* List review statistics: Get statistical data about the activity of the reviewers.
  ```shell
  gh tutorbot list-review-statistics <assignment> [--classroom <classroom>] [--sort-by (Reviewer|CommentLength|ReviewDate)]
  ```

* Getting help
  ```shell
  gh tutorbot [<command>] --help
  ```

## Configuration
* `classroom_roster.cvs`: The student's roster file contains the list of students in a classroom.
  It's main purpose is to map the GitHub username to the student's id (matriculation number) and name.
  The roster file can be downloaded from the classroom page on GitHub in the following way: Classroom page → Students → Download.
  Place the file in your working directory and name it `classroom_roster.cvs`.

* `appsettings.json`: This configuration file contains general settings for the .NET application and TutorBot specific settings.
  ```json
  {
  "Logging": {
    "LogLevel": {
      "Default": "Error"
    }
  },

  "default-classroom": "swo3-vz"
  }
  ```
  + `default-classroom`: The default value for the `--classroom` option.


## Working with TutorBot

1. Check if you are a member with admin permissions in the classroom you want to work with. 
   If not, ask the classroom owner to add you as a member with the needed permissions.
2. Install GitHub CLI and TutorBot as described above.
3. Download the roster file to your working directory. Create the configuration file `appsettings.json` and define the default classroom.
4. Authenticate with GitHub CLI:
   ```shell
   gh auth login
   ```
   You can check your authentication status with
   ```shell
   gh auth status
   ```
   TutorBot will use the generated security token to access the GitHub API.
5. Apply the commands described above to your classroom.

## Typical workflow for handling an assignment

1. Create a new assignment in the classroom.
   * Individual assignment
   * Private repository
   * Do not grant admin permissions to students
   * Optional: Add a starter code repository
   * Important: Enable feedback pall requests
2. Make invitation link available to students (e. g. via Moodle).
3. After the assignment deadline, assign reeviews to students (`gh tutorbot assign-reviewers`).
4. Reviewers (students) give feedback
   * Visit the repository of the submission in the web broswser
   * Navigate to the *Pull requests* tab and select *Feedback* (`<repository-url>/pulls/1`)
   * On the *Feedback* page select *Files changed*
   * Add comments to the code and the documentation
     + Click on the line number to add a comment to a specific line. You can also select a range of lines to add a comment to multiple lines.
       Comments can also be attachted to a whole file by clicking on the balloon icon in the upper right corner of the file.
     + After adding the first comment, click on *Start a review* to add more comments.
     + Add more comments by selecting lines in the way described above and clicking on *Add comment*. 
     + Finish your review by clicking on *Finish your review*. In the popup window you must leave a comment for the whole review. Finally click on *Submit review*.
     + You can add multiple reviews to a pull request. To add another review, click on *Start a review* again.
   * See [Commenting on a pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/commenting-on-a-pull-request) 
     in the GitHub documentation for more details.
5. Perfom a plagiarism check. Download all submissions before (`gh tutorbot clone-assignment`)
6. Give feedback (tutors, professors)
7. Download the self assessments (`gh tutorbot download-assessments`).
