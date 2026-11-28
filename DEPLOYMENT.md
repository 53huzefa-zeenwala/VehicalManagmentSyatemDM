# Deployment — Backend API

**Version:** 1.0 · **Date:** 2026-11-27 · **Scope:** deploying `backend/src/VehicleManagementDM.Api` to the existing Windows server that already runs the Sufizz Bikes backend (D-028). No secrets, server addresses or passwords belong in this file: the repository is public.

## 1. What the server needs

| Need | Status |
|---|---|
| ASP.NET Core **8** runtime (hosting bundle for IIS) | Installed: **8.0.18** (`Microsoft.AspNetCore.App` and `Microsoft.NETCore.App`, checked 2026-11-27, D-050). Our build runs on any 8.0.x patch |
| SQL Server | The server's SQL Server 2019 Express (D-029). Nothing to install |
| Anything else | Nothing. All NuGet packages ship inside the publish folder |

## 2. Build the release on the dev machine

```powershell
cd backend
dotnet test
dotnet publish src/VehicleManagementDM.Api -c Release /p:PublishProfile=FolderProfile
```

Output: `backend/src/VehicleManagementDM.Api/bin/Release/net8.0/publish/` (framework-dependent, Release). `appsettings.Development.json` is excluded on purpose.

## 3. One-time server setup

### 3.1 Folders (outside the site folder, so a deploy never touches them)

| Folder (example) | Setting | Holds |
|---|---|---|
| `D:\VehicleManagementDM\site` | — | The published files (replaced on each deploy) |
| `D:\VehicleManagementDM\data\files` | `Storage__RootPath` | Photos and signatures (D-034) — **back up** |
| `D:\VehicleManagementDM\data\logs` | `ErrorLogging__FallbackDirectory` | Error logs when the database is down |
| `D:\VehicleManagementDM\data\keys` | `DataProtection__KeysPath` | Cookie encryption keys — without them every restart signs everyone out |

Give the IIS application-pool identity (or the service account) **modify** rights on the three `data` folders.

### 3.2 Database and login (OI-12)

The API creates its database and tables itself on first start (D-030). Run once in SSMS as an administrator, replacing the placeholders:

```sql
CREATE DATABASE [VehicleManagementDM];
GO
CREATE LOGIN [vms_app] WITH PASSWORD = N'<strong password>', CHECK_POLICY = ON;
GO
USE [VehicleManagementDM];
CREATE USER [vms_app] FOR LOGIN [vms_app];
ALTER ROLE [db_owner] ADD MEMBER [vms_app];  -- needed to apply migrations at startup; this database only
GO
```

**Owner's choice (D-052): production connects with `sa`**, so this section is optional (the API creates the database itself on first start). If `vms_app` is set up later, only the connection string changes. With `sa`: use a long random password, restrict port 1433 to known IPs in the server firewall (the API connects via `localhost` and doesn't need it open), and limit who can read the site folder.

### 3.3 Site and settings

**IIS (in-process, as the Sufizz Bikes backend):**

1. New application pool: *No Managed Code*, start mode *AlwaysRunning*, **Idle Time-out (minutes) = 0** (Advanced Settings). On the site: **Preload Enabled = True**. Together they keep the API (and the reminder job) running; without them IIS stops the app after 20 idle minutes and the next request has to start it again.
2. New site pointing at `...\site`, HTTPS binding with the server's certificate on port **8445** (D-050).
3. Give the API its settings, with **one** of these two ways.

**Way 1 (simplest): `appsettings.Production.json` in the site folder.** Create it once on the server next to `appsettings.json`. The publish output never contains this file (excluded in the project and in git), so copying a new release over the site folder keeps it; never empty the site folder before a deploy. JSON needs double backslashes in paths:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=VehicleManagementDM;User ID=sa;Password=<sa password>;TrustServerCertificate=True;Encrypt=True"
  },
  "Storage": { "RootPath": "D:\\VehicleManagementDM\\data\\files" },
  "ErrorLogging": { "FallbackDirectory": "D:\\VehicleManagementDM\\data\\logs" },
  "DataProtection": { "KeysPath": "D:\\VehicleManagementDM\\data\\keys" }
}
```

The environment is `Production` when nothing sets it, so this file is read automatically. Every other setting keeps its default from `appsettings.json` (session 12 h / 30 days, lockout 5 tries / 15 min, photos 20 MB, error logs 30 days, Swagger off).

**Way 2: environment variables** for the site, in the server's own `web.config` `<environmentVariables>` (never committed) or as app-pool environment variables. They override the JSON files:

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | `Server=<host>,1433;Database=VehicleManagementDM;User ID=vms_app;Password=<…>;TrustServerCertificate=True;Encrypt=True` |
| `Storage__RootPath` | e.g. `D:\VehicleManagementDM\data\files` |
| `ErrorLogging__FallbackDirectory` | e.g. `D:\VehicleManagementDM\data\logs` |
| `DataProtection__KeysPath` | e.g. `D:\VehicleManagementDM\data\keys` |
| `Seed__FirstAdmin__FullName`, `__Mobile`, `__Password` | Optional: only for the very first start (creates the first admin when none exists, AUTH-06). Remove them afterwards. Or create the first admin with SQL (§3.4) |
| `Swagger__Enabled` | Leave unset (off) in production |

The shipped `web.config` already makes IIS pass the API's JSON errors through untouched and allows 21 MB requests for photo uploads.

### 3.4 First admin with SQL (instead of the seed variables)

Run **after** the API has started once (it creates the tables). Passwords are stored as plain text (D-051). Fill in the three values (password 8–128 characters; mobile in the same format the app uses), run it in SSMS, and note the login ID it prints:

```sql
USE [VehicleManagementDM];
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @FullName nvarchar(100) = N'<full name>';
DECLARE @Mobile   varchar(20)   = '<mobile, e.g. +971501234567>';
DECLARE @Password nvarchar(128) = N'<password>';

DECLARE @Number int;
UPDATE [IdSequences] SET @Number = [LastNumber] = [LastNumber] + 1 WHERE [Role] = 'Admin';

DECLARE @LoginId varchar(20) =
    'ADM' + RIGHT(CONVERT(varchar(4), YEAR(DATEADD(HOUR, 4, SYSUTCDATETIME()))), 2)
          + RIGHT('00000' + CONVERT(varchar(5), @Number), 5);

INSERT INTO [Users] ([LoginId], [Role], [FullName], [Mobile], [Password], [SecurityStamp],
                     [FailedLoginCount], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
VALUES (@LoginId, 'Admin', @FullName, @Mobile, @Password, LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
        0, 1, SYSUTCDATETIME(), SYSUTCDATETIME());

COMMIT TRANSACTION;
SELECT @LoginId AS LoginId;
```

The ID counter is updated in the same transaction, so the API never issues the same ID again.

To set a password directly (also unlocks the account and signs that user out everywhere):

```sql
UPDATE [Users]
SET [Password] = N'<new password>',
    [SecurityStamp] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
    [FailedLoginCount] = 0,
    [LockoutEndUtc] = NULL
WHERE [LoginId] = '<login ID>';
```

## 4. Deploy (each release)

1. Stop the site (or drop `app_offline.htm` into `...\site`).
2. Copy the publish output over `...\site`.
3. Start the site. On start the API applies any new migrations (D-030); while it does, requests get `503 DATABASE_NOT_READY` in JSON.
4. Check: `GET https://<server>:<port>/health` → `{"status":"Healthy","database":"Healthy"}`.
5. First deploy only: create the first admin, either with the seed variables (read the login ID from the startup log `First admin account created with login ID …`, then remove the variables) or with SQL (§3.4). Sign in and change the password.

**Release of 2026-11-28 (D-056):** the API drops the lot latitude/longitude columns (migration `RemoveLotCoordinates`, applied on start). Older staff APKs keep working, but their Lots "Map" button would open a wrong place, so give staff the new APK (no Map button) with this release. The new apps also work with the older API.

## 4a. Test data (optional)

After the first admin exists, run on any PC with PowerShell 7:

```powershell
pwsh -File tools/seed-test-data.ps1 -BaseUrl https://<server>:8445 -LoginId <admin ID>
```

It asks for the admin password (never stored), checks the sign-in, and creates through the API: categories *Test Car* / *Test Bike* with 4 types, lots `TEST-A` and `TEST-B` with capacities, areas *Test Downtown* / *Test Marina* with 4 locations, vehicles `TEST-1001`…`TEST-1005` (one Under maintenance) and staff *Test Driver One/Two*. It skips anything that already exists, so it is safe to run again. The new staff IDs and their shared password are printed once at the end.

## 5. Backups (both are needed)

| What | How | When |
|---|---|---|
| Database | SQL Server Express has no Agent: schedule a Windows Task that runs `sqlcmd -S <host> -E -Q "BACKUP DATABASE [VehicleManagementDM] TO DISK = N'<path>\VehicleManagementDM_%date%.bak' WITH COMPRESSION, CHECKSUM"` (or an SSMS maintenance script) | Daily |
| Photos and signatures | Copy `data\files` (e.g. `robocopy /MIR`) to backup storage | Daily, with the database |

Test a restore of both together at least once before go-live (REQUIREMENTS.md §8, data protection).

## 6. Operations

| Topic | Where |
|---|---|
| Health | `GET /health` (no sign-in) |
| Errors | `ErrorLogs` table (30 days), or `data\logs\errors-YYYY-MM-DD.jsonl` when the database was down. Every error the app shows carries a `traceId` that finds its row |
| Reminders | Background job every minute, inside the API process. With IIS, keep the app pool *AlwaysRunning* and disable idle time-out |
| Push notifications | Off until a Firebase project exists (D-039); users see notifications in the app's list |
| Database size | Express limit 10 GB per database; error logs are pruned after 30 days; photos are on disk, not in the database |

## 7. Pre-go-live checklist

- [x] Server runtime is ASP.NET Core 8.0.18, hosted in IIS (D-050). .NET 8 is out of Microsoft support (OI-09, D-008); installing the last 8.0 patch on the server is advised.
- [ ] `appsettings.Production.json` in the site folder with the real `sa` password (D-052); port 1433 restricted to known IPs.
- [ ] The three data folders exist with write permission; Data Protection keys persist across a site restart (sign in, restart, still signed in).
- [ ] HTTPS binding works from a phone on mobile data (the app accepts the self-signed certificate, D-036).
- [ ] `/health` is Healthy; an unknown URL returns JSON 404, not an IIS page.
- [ ] First admin created (seed or SQL §3.4), password changed, seed variables removed if used.
- [ ] Database access limited to trusted people and backup files protected: passwords are plain text in `Users` (D-051).
- [ ] Database and file backups scheduled, and one restore tested.
