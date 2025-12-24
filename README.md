# Sharenote Gainsight Staff Export

## Overview
This project exports staff/provider data from the ShareNote database and uploads it to Gainsight via SFTP.

The job:
- Runs on **quarter start dates** (Jan 1, Apr 1, Jul 1, Oct 1)
- Fetches staff data using a **SQL Server stored procedure**
- Generates a **CSV report**
- Uploads the file to **SFTP**
- Archives or marks files as failed based on upload result

---

## Technology Stack
- .NET 8
- C#
- Dapper
- SQL Server (Stored Procedures)
- CsvHelper
- SSH.NET (SFTP)
- NLog
- Worker Service

---

## Project Structure
SharenoteGainsightReport
│
├── Domain
│ └── StaffRecord.cs
│
├── DataAccess
│ └── StaffRepository.cs
│
├── Infrastructure
│ ├── Csv
│ ├── Sftp
│ └── Files
│
├── Services
│ └── StaffExportService.cs
│
├── appsettings.json
├── nlog.config
└── README.md


---

## Configuration

### Required appsettings.json values
## json
{
  "Paths": {
    "RootPath": "C:\\Jobs\\SharenoteGainsight",
    "Reports": "Reports",
    "Archive": "Archive",
    "Failed": "Failed",
    "Logs": "Logs"
  },
  "Sql": {
    "ConnectionString": "SQL_CONNECTION_STRING"
  },
  "SFTP": {
    "Host": "SFTP_HOST",
    "Port": 22,
    "UserName": "USERNAME",
    "Password": "PASSWORD",
    "RemoteDirectory": "TEST/Sharenote/{0}",
    "TimeoutSeconds": 180
  }
} 

## Database Access

- Data is fetched using SQL Server stored procedures
- Inline SQL is intentionally avoided
- Stored procedure names are controlled via enums for compile-time safety

Example:
- dbo.usp_GetProviderListGainsight


## How to Run

### Local
1. Update `appsettings.json`
2. Build the solution
3. Run the Worker project

Outputs:
- Logs → Logs folder
- CSV → Reports folder
- SFTP upload → remote server

### Production
- Runs as a scheduled worker job
- Includes retry logic for SFTP upload
- Gracefully handles failures


## Retry & Failure Handling

- SFTP upload retries are supported
- On success → file moved to Archive
- On failure → file moved to Failed
- No partial files are uploaded