# RaceDay RESTful API

## Project Overview

RaceDay is a RESTful Web API developed using ASP.NET Core and C#. It supports event management for road running, walking and cycling events. The API provides functionality for user authentication, event management, participant enrolment and race results.

## Technologies Used

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Swagger / OpenAPI
- xUnit for unit testing
- GitHub Actions for continuous integration

## User Roles

**Organiser**
- Manage events.
- Access enrolment information for their events, subject to the implemented access controls.

**Participant**
- View available events.
- Enrol in event categories.
- Access functionality permitted to authenticated participants.

## Main API Features

- User registration and login.
- Organiser and Participant roles.
- Password hashing during registration.
- JWT-based authentication.
- Event management.
- Categories and enrolments.
- Race results.
- API endpoint documentation through Swagger.

## Getting Started

### Prerequisites

Install the following tools:

- .NET 8 SDK
- Visual Studio 2022 or a compatible IDE
- SQL Server

### Setup Instructions

1. Clone or download this repository.
2. Open `RaceDayAPI.sln` in Visual Studio.
3. Check the database connection string in `appsettings.json`.
4. Ensure SQL Server is running and the database is configured.
5. Restore NuGet packages.
6. Build the solution.
7. Run the `RaceDayAPI` project.
8. Open the Swagger URL displayed by the application to explore and test the API endpoints.

### Running Unit Tests

In Visual Studio:

1. Open **Test → Test Explorer**.
2. Select **Run All Tests**.
3. Confirm that the tests pass.

Alternatively, run the following commands from the solution folder:

```bash
dotnet restore
dotnet build
dotnet test
```

## Continuous Integration

This repository uses GitHub Actions to build the project when changes are pushed to GitHub. The workflow is defined in `.github/workflows/dotnet.yml`.

Check the **Actions** tab in GitHub to view workflow results.

## Repository

GitHub: https://github.com/precious554/RaceDayAPI
