<br />
<div align="center">
  <h1>DevOps-GroupC</h3>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#the-application">The application</a></li>
        <li><a href="#prerequisites">Prerequisites</a></li>
      </ul>
    </li>
    <li>
      <a href="#running-the-project">Running the project</a>
      <ul>
        <li><a href="#run-using-docker-the-prefered-method">Using docker (the prefered method)</a></li>
        <li><a href="#run-using-the-dotnet-runtime-directly">Using the dotnet runtime directly</a></li>
        <li><a href="#monitoring">Monitoring</a></li>
        <li><a href="#testing">Testing</a></li>
      </ul>
    </li>
    <li><a href="#how-to-contribute">How to contribute</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>

## Enegery Measurement
To run the project, ensure that a postgres database is running, navigate to [csharp-minitwit/](./csharp-minitwit/) and run `dotnet run`

## About The Project
This project is part of the course [DevOps, Software Evolution and Software Maintenance, MSc (Spring 2024)](https://learnit.itu.dk/local/coursebase/view.php?ciid=1391)

The project currently consists of three main folders:
- **csharp-minitwit**, which is a ported version of itu-minitwit, made in C#.
- **infrastructure**, Containing shell and Terraform scripts for provisioning.
- **report**, Containing a report written in LaTex, generated as a pdf in the build folder.


### The application
The 'csharp-minitwit' application is a miniature version of X (formerly known as Twitter). 

### Prerequisites
* Linux. It is also possible to work on the project using MacOS, Windows has a few Git issues with the 'infrastructure/secrets:Zone.Identifier' file.
* The [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). There are multiple options for downloading depending on your operating system. If on linux, we recommend the  [scripted install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install).
* SQLite, multiple guides can be found online on how to install, this highly depends on which operating system is used.
* [Docker](https://docs.docker.com/engine/install/)
* Python with the pytest and request pip-packages installed (only needed for testing).

<!-- USAGE EXAMPLES -->
## Running the project
This project can be run in two ways:

### Run using docker (the prefered method):
Run using `docker` - runs only the app:
```sh
docker build -t csharp-minitwit .
docker run -p 5000:8080 csharp-minitwit
```
At this point, the application can be accessed using the link provided in the terminal (http://localhost:5000).

Run using `docker-compose` - runs multiple development services, such as Prometheus, Grafana, etc.:
```sh
docker-compose up
```

### Run using the dotnet runtime directly:
This firstly requires a few changes to the code. The default connection string points to a folder which will be generated in the docker container normally. 
Change the connection string in 'appsettings.Development.json', from 'Data Source=/app/Databases/volume/minitwit.db' to 'Data Source=./Databases/volume/minitwit.db' ***DO NOT COMMIT THIS***.

cd into the correct folder:
```sh
cd csharp-minitwit
```
Run using `dotnet`:
```sh
dotnet run
```



### Monitoring
| Service    | Endpoint |
| -------- | ------- |
| Built-in metrics  | http://localhost:5000/metrics    |
| Prometheus | http://localhost:9090     |
| Grafana    | http://localhost:3000    |


### Testing
When developing APIs locally, Swagger is setup at http://localhost:5000/swagger/index.html for easy manual testing.

Unit tests for `csharp-minitwit` can be run by opening a new terminal and cd into the test folder:
```sh
cd csharp-minitwit/Tests
```
And running the tests using Pytest:
```sh
pytest refactored_minitwit_tests.py
pytest minitwit_sim_api_test.py
```

## How to contribute
For this repository we try to follow the [GitFlow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow) workflow:
* Create an issue
* Create a branch using the GitHub issue tracker to ensure correct naming (remember to prefix the branch name with feature/{issue_name})
* Develop feature
* Create a pull request

<!-- ACKNOWLEDGMENTS -->
## Acknowledgments
Shout out to [ChatGPT](https://chatgpt.com/) for help with debugging.
