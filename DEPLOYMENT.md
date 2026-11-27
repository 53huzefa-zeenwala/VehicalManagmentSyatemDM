# Deployment — Backend API

**Version:** 1.0 · **Date:** 2026-11-27 · **Scope:** deploying `backend/src/VehicleManagementDM.Api` to the existing Windows server that already runs the Sufizz Bikes backend (D-028). No secrets, server addresses or passwords belong in this file: the repository is public.

## 1. What the server needs

| Need | Status |
|---|---|
| ASP.NET Core **8** runtime (hosting bundle for IIS) | Already there for the Sufizz Bikes backend (D-028). Confirm the exact patch with `dotnet --list-runtimes` (OI-13) |
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

Never use `sa` in production. Consider restricting port 1433 to known IPs in the server firewall.

### 3.3 Site and settings

**IIS (in-process, as the Sufizz Bikes backend):**

1. New application pool: *No Managed Code*, start mode *AlwaysRunning* (keeps reminders running).
2. New site pointing at `...\site`, HTTPS binding with the server's certificate on a free port (OI-13; the Sufizz Bikes backend already uses its own ports).
3. Set the environment variables below for the site, either in the server's own `web.config` `<environmentVariables>` (never committed) or as app-pool environment variables.

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | `Server=<host>,1433;Database=VehicleManagementDM;User ID=vms_app;Password=<…>;TrustServerCertificate=True;Encrypt=True` |
| `Storage__RootPath` | e.g. `D:\VehicleManagementDM\data\files` |
| `ErrorLogging__FallbackDirectory` | e.g. `D:\VehicleManagementDM\data\logs` |
| `DataProtection__KeysPath` | e.g. `D:\VehicleManagementDM\data\keys` |
| `Seed__FirstAdmin__FullName`, `__Mobile`, `__Password` | Only for the very first start (creates the first admin when none exists, AUTH-06). Remove them afterwards |
| `Swagger__Enabled` | Leave unset (off) in production |

The shipped `web.config` already makes IIS pass the API's JSON errors through untouched and allows 21 MB requests for photo uploads.

## 4. Deploy (each release)

1. Stop the site (or drop `app_offline.htm` into `...\site`).
2. Copy the publish output over `...\site`.
3. Start the site. On start the API applies any new migrations (D-030); while it does, requests get `503 DATABASE_NOT_READY` in JSON.
4. Check: `GET https://<server>:<port>/health` → `{"status":"Healthy","database":"Healthy"}`.
5. First deploy only: read the first admin's login ID from the startup log (`First admin account created with login ID …`), sign in from the app, change the password, then remove the `Seed__FirstAdmin__*` variables.

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

- [ ] Server runtime is ASP.NET Core 8.0.x; record the patch level (OI-13). .NET 8 is out of Microsoft support (OI-09, D-008).
- [ ] Database `VehicleManagementDM` and login `vms_app` created; `sa` not used (OI-12).
- [ ] The three data folders exist with write permission; Data Protection keys persist across a site restart (sign in, restart, still signed in).
- [ ] HTTPS binding works from a phone on mobile data (the app accepts the self-signed certificate, D-036).
- [ ] `/health` is Healthy; an unknown URL returns JSON 404, not an IIS page.
- [ ] First admin created, password changed, seed variables removed.
- [ ] Database and file backups scheduled, and one restore tested.
