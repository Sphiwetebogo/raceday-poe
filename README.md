# RaceDay 

RaceDay lets **Organisers** publish running, walking and cycling events and capture results, and lets **Participants**
browse events, enter a category, track their entries and view their results.

## User Roles
### Organiser
Registers with a secret organiser code, then manages events (with banner images), categories, enrolments and results.
### Participant
Sees an athlete dashboard (total, confirmed and pending enrolments), browses and filters upcoming events, enters an event by choosing a category, cancels entries, views results and
maintains a profile with a profile picture.

## Solution Structure
```
RaceDay
|-- .github/workflows/ci.yml        CI: restore, build whole solution (API + MVC + tests), run tests
`-- Part2
    |-- RaceDay.sln                 API + Tests + MVC
    |-- RaceDay.Api                 Part 2 REST API (+ Part 3 Azure Blob image upload)
    |-- RaceDay.Api.Tests           xUnit tests
    |-- RaceDay.Mvc                 Part 3 ASP.NET Core MVC web front end (+ Dockerfile)
    `-- database                    schema-part2.sql, schema-part3.sql
```
`RaceDay.Mvc` talks to the database and Azure **only through** `RaceDay.Api`. It has no EF Core or Azure code.

## Part 1 - Planning and Database
ERD, API plan and the SQL script (see the Part 1 submission). `Part2/database/schema-part2.sql` is the script plus the
Part 2 columns; `schema-part3.sql` adds the two image URL columns.

## Part 2 - RESTful API
ASP.NET Core Web API, EF Core code-first on SQL Server, server-side session authentication, role-based authorisation,
Swagger at `/swagger`, xUnit tests.

## Part 3 - MVC Web Application
ASP.NET Core MVC (.NET 8) with Bootstrap 5, session-based login, role-specific menus, Azure Blob Storage image uploads
(through the API) and Docker.

## Prerequisites
.NET 8 SDK, SQL Server / LocalDB, an Azure Storage account, Docker Desktop (Linux containers, WSL 2).

## Database Setup
Nothing to run by hand. When the API starts it creates `RaceDayDB` if it is missing and adds the two Part 3 columns
(`Events.BannerImageUrl`, `Users.ProfileImageUrl`) to an existing Part 2 database. `Part2/database/schema-part3.sql`
contains the same statements if you ever want to run them manually.

## How to Run RaceDay.Api
```
cd Part2
dotnet run --project RaceDay.Api --urls http://0.0.0.0:5080
```
Swagger: http://localhost:5080/swagger  (`0.0.0.0` lets the Docker container reach the API on the host.)

## How to Run RaceDay.Mvc Locally
1. Start the API (above).
2. `RaceDay.Mvc/appsettings.json` -> `ApiSettings:BaseUrl` is `http://localhost:5080/`.
3. `dotnet run --project RaceDay.Mvc` (opens http://localhost:5090) or press F5 in Visual Studio.

Registering as an **Organiser** needs the organiser code configured in `RaceDay.Api/appsettings.json`
(`Registration:OrganiserCode`).

## Azure Blob Storage Setup
1. Create a Storage account and (optionally) a container `raceday-images`. The API creates it if missing.
2. To let browsers display the images the container needs anonymous **blob** read access (the API creates it that way
   when `AzureBlobStorage:PublicRead` is `true`). The storage account must allow anonymous access; use a demo account.
3. Keep the connection string out of GitHub - use user secrets:
```
cd Part2/RaceDay.Api
dotnet user-secrets init
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your storage connection string>"
```
4. Flow: MVC form (multipart) -> `POST /api/events/{id}/banner` or `POST /api/users/profile/image` ->
   API uploads to Blob Storage -> URL saved in the database -> MVC shows the image.

## Docker Setup
Run from the `Part2` folder (the one containing `RaceDay.sln`). Start the API on the host first (see above).

### Build Image
```
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 .
```
### Run Container
```
docker run -d --name raceday-mvc -p 8080:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5080/ raceday-mvc:part3
```
Open http://localhost:8080. Inside the container `localhost` is the container itself, so it reaches the API on the host
via `host.docker.internal`. Useful: `docker ps`, `docker logs raceday-mvc`, `docker rm -f raceday-mvc`.

## Authentication and Session Flow
1. Login posts to `api/auth/login`. The API starts its session and sets the `.RaceDay.Session` cookie.
2. MVC stores `UserId`, `Role`, `DisplayName` and that API cookie in its own session and sends the cookie on every API call.
3. `[SessionAuthorize]` / `[SessionAuthorize("Organiser")]` protect pages: not logged in -> Login, wrong role -> Access Denied.
4. Logout calls `api/auth/logout`, clears the MVC session and returns Home.

## Organiser Features
Dashboard (own events, upcoming count, enrolments), event create/edit/delete with banner upload, categories (name,
distance, fee), enrolments per event with status badges and a Confirm button (marks an enrolment Paid/Confirmed), results capture (hh:mm:ss + position, only after the event date).

## Participant Features
Upcoming events with search and type filter (done by the API), event details with categories and **Enter Event**,
My Events with cancel, My Results (time, position of total finishers), Profile with picture upload.

## CI/CD
`.github/workflows/ci.yml` restores, builds `Part2/RaceDay.sln` (API, tests and MVC) and runs the tests.

## GitHub Actions Screenshot
_Insert screenshot of the green workflow run here._

## Video Presentation
_Insert video link here._
