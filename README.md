﻿# GitHub Tutorbot

*GitHub Tutorbot* is a simple command line tool that helps programming tutors at the University of Applied Sciences in Hagenberg
by automating repetitive tasks.


## Installation

1. Tutorbot is an extension for the [GitHub CLI](https://cli.github.com/), so you need to install it first. Installation instructions
   can be found [here](https://github.com/cli/cli#installation). Prepackaged binaries can be found [here](https://github.com/cli/cli/releases/).

2. Install the Tutorbot extension by running the following command:
   ```shell
   gh extension install https://github.com/jheinzel/gh-tutorbot
   ```

## Commands

Tutorbot comes with the following range of commands:

* List all classrooms the user is a member of:
  ```shell
  gh-tutorbot list-classrooms
  ```

  * List all assignments created in a classroom :
  ```shell
  gh-tutorbot list-assignments [--classroom <classroom>]
  ```

* List all submissions of an assignment:
  ```shell
  gh-tutorbot list-submissions <assignment> [--classroom <classroom>]
  ```

* Assign reviewers to an assignment: One reviewer per submission is assigned in a random fashion.
  The reviewer gets read access to the submission repository and gets an invitation via email.
  ```shell
  gh-tutorbot assign-reviewers <assignment> [--classroom <classroom>]
  ```

* Assign reviewers from an assignment: 
  ```shell
  gh-tutorbot remove-reviewers <assignment> [--classroom <classroom>]
  ```

* Clone all repositories of an assignment: The directory the repositories are cloned to can be specified.
  If omitted, the current working directory is used. The directory is created if it does not exist.
  If the directory ist not empty, the command will fail.
  ```shell
  gh-tutorbot clone-assignment <assignment> [--directory <directory>] [--classroom <classroom>]
  ```

* Getting help
  ```shell
  gh-tutorbot [<command>] --help
  ```

## Configuration
* `classroom_roster.cvs`: The student's roster file contains the list of students in a classroom.
  It's main purpose is to map the GitHub username to the student's id (matriculation number) and name.
  The roster file can be downloaded from the classroom page on GitHub in the following way: Classroom page → Students → Download.
  Place the file in your working directory and name it `classroom_roster.cvs`.

* `appsettings.json`: This configuration file contains general settings for the .NET application and Tutorbot specific settings.
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


## Working with Tutorbot

1. Check if you are a member with admin permissions in the classroom you want to work with. 
   If not, ask the classroom owner to add you as a member with the needed permissions.
2. Install GitHub CLI and Tutorbot as described above.
3. Download the roster file to your working directory. Create the configuration file `appsettings.json` and define the default classroom.
4. Authenticate with GitHub CLI:
   ```shell
   gh auth login
   ```
   You can check your authentication status with
   ```shell
   gh auth status
   ```
   Tutorbot will use the generated security token to access the GitHub API.
5. Apply the commands described above to your classroom.
