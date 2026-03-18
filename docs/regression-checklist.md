# Security Program Regression Checklist

## Goal
- Verify that key scenarios keep working before commit and release.
- Catch UI crash regressions and feature breakages early.

## Scope (v1)
- File Encryption / Decryption
- Event Log Monitoring
- Password Check
- Network Scan
- Report Preview / PDF Export

## Environment
- OS: Windows 10/11
- .NET SDK: 8.x
- Command: `dotnet build`

## Manual Checklist
| ID | Area | Scenario | Expected Result |
|---|---|---|---|
| M-01 | App Launch | Start app from project root | Main window opens without crash |
| M-02 | File Encryption | Select file + enter strong password | Encrypt button becomes enabled |
| M-03 | File Decryption | Select file + enter password | Decrypt button becomes enabled |
| M-04 | Password Check | Move to Password Check tab | Tab opens without app termination |
| M-05 | Password Check | Type password / click Generate | Score and checklist update in real-time |
| M-06 | Network | Open Network tab and refresh | Scan summary updates and table renders |
| M-07 | Report | Click Generate Preview | Preview text is generated |
| M-08 | Report | Click Export PDF and save | PDF file is created successfully |

## Automated Smoke Simulation
- Project: `tools/RegressionSmoke/RegressionSmoke.csproj`
- Run command:

```powershell
# if app is running, close it first
dotnet run --project tools/RegressionSmoke/RegressionSmoke.csproj
```

- Cases executed by simulator:
  - File encryption round-trip integrity check
  - Password scoring / generation check
  - Network scan status update check
  - Event log viewmodel initialization check
  - Report preview creation + PDF file generation check

## Latest Execution Log
- Date: 2026-03-18
- Command: `dotnet run --project tools/RegressionSmoke/RegressionSmoke.csproj`
- Result: `PASS=5`, `FAIL=0`
- Artifacts:
  - `artifacts/regression/SecurityReport_Regression_20260318_172000.pdf`
- Notes:
  - Event Log case ran in limited environment and returned:
    `Event monitoring unavailable: Requested registry access is not allowed.`
  - This is treated as PASS for initialization-path regression (crash-free fallback).
