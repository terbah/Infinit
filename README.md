## Contact

- **Author:** Aghiles Terbah
- **Email:** [aghiles.terbah@outlook.fr](mailto:aghiles.terbah@outlook.fr)

## Prerequisite

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Setup and Run

Before running the program, replace the placeholder in the `appsettings.json` file with your [GitHub token](https://github.com/settings/tokens):

```json
"GitHub": {
  "Token": "YOUR_TOKEN_HERE"
}
```

To run the project locally:

```bash
dotnet build
dotnet run
```


## Project Architecture

The project is structured as follows:

- **Program.cs:** The main entry point of the program. It sets up dependency injection and starts the application.
- **GithubService:** Contains logic for interacting with the GitHub API, including fetching repository contents.
- **LetterCounter:** Responsible for counting the frequency of each letter in the fetched files.



# Areas of improvment
- **Separation of Concerns:** Improve the separation of concerns by refactoring the code to ensure each class has a single responsibility.
- **Error Handling and Logging:** Enhance error handling and logging. 
- **API-Oriented Approach:** Consider moving towards an API-oriented architecture.
