# GlucoTrack Backend API

> ASP.NET Core Web API powering GlucoTrack: secure role‑based clinical data, alert logic, adherence evaluation & auditability.

## ✅ Implemented Scope (Core Complete)

| Domain               | Implemented Highlights                                                                                                       |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| Auth / Roles         | Login, role resolution (Admin / Doctor / Patient), guarded endpoints                                                         |
| Users                | CRUD (admin‑scoped), role assignment, soft lifecycle awareness                                                               |
| Clinical Data        | Glycemic measurements, therapies, medication schedules & intakes, symptoms, reported conditions, risk factors, comorbidities |
| Alerts               | Manual creation (glycemia), unresolved retrieval, resolution workflow, severity mapping & recipient scoping                  |
| Analytics            | Doctor dashboard summary (aggregated patient adherence / alert snapshots)                                                    |
| Auditing             | ChangeLogs entity schema & insertion points (extensible)                                                                     |
| Monitoring Interface | Foundations enabling external monitoring task (periodic anomaly checks)                                                      |
| Validation           | Layered: DTO syntactic validation + semantic business rules                                                                  |

Core is complete; below items are optional evolutions.

## 🔭 Optional Roadmap

| Area               | Idea                                                                   |
| ------------------ | ---------------------------------------------------------------------- |
| Contract Safety    | Versioned routes (`/api/v1`) + future backward compatibility tests     |
| Security Hardening | JWT refresh rotation, secret vault integration, stricter claim scoping |
| Observability      | Structured logging enrichment + OpenTelemetry traces                   |
| Performance        | Caching read‑heavy aggregates (doctor dashboard)                       |
| Predictive         | Risk scoring endpoints (hypo/hyper trend prediction)                   |

## 🧱 Architecture

```
Controllers  -->  (inline service logic, candidate extraction)  -->  EF Core DbContext
            DTOs (transport)      Entities (persistence)
```

Principles:

- Separation of concerns between transport (DTOs) and persistence (Entities).
- Explicit role checks at controller entry.
- Semantic validation aggregated before state mutation.
- Alert logic centralized to keep consistent severity & recipient rules.

## 📂 Key Folders

| Folder                   | Purpose                                                  |
| ------------------------ | -------------------------------------------------------- |
| Controllers              | HTTP endpoints (Auth, Admin, Patient, Doctor, Alert, Db) |
| DTOs                     | Transport models (future versioning surface)             |
| Models                   | EF Core entity classes (relational mapping)              |
| Data                     | `GlucoTrackDBContext` (DbSets, configuration)            |
| Utils                    | Helper utilities (mapping / constants)                   |
| Tests (separate project) | xUnit unit tests                                         |

## 🗄 Data Model (Abbreviated)

Users, Roles, Therapies, MedicationSchedules, MedicationIntakes, GlycemicMeasurements, Symptoms, ClinicalComorbidities, RiskFactors, ReportedConditions, Alerts, AlertTypes, AlertRecipients, ChangeLogs, PatientDoctors, PatientRiskFactors.

Integrity:

- Foreign keys, required constraints, uniqueness where clinically needed.
- Application layer semantic checks (e.g. schedule vs intake consistency).
- Soft deletion patterns prepared for historical continuity (where applicable).

## 🌐 Representative Endpoints

| Purpose                       | Method & Path                                 |
| ----------------------------- | --------------------------------------------- |
| Login                         | POST `/Auth/login`                            |
| List Users                    | GET `/Admin/users`                            |
| Upsert User                   | POST `/Admin/user`                            |
| Delete User                   | DELETE `/Admin/user?userId=`                  |
| Clinical Info (self/doctor)   | GET `/Auth/info?userId=`                      |
| Unresolved Alerts             | GET `/Alert/user-not-resolved-alerts?userId=` |
| All Alerts                    | GET `/Alert/user-alerts?userId=`              |
| Create Manual Glycemia Alert  | POST `/Alert/create-glycemia-alert`           |
| Resolve Alert                 | POST `/Alert/resolve-alert?alertRecipientId=` |
| Patient Daily Resume          | GET `/Patient/daily-resume?userId=&date=`     |
| Patient 7-Day Glycemia Resume | GET `/Patient/glycemic-resume?userId=`        |
| Doctor Dashboard Summary      | GET `/Doctor/dashboard-summary?doctorId=`     |
| Recent Therapies              | GET `/Doctor/recent-therapies?doctorId=`      |
| Create / Update Therapy       | POST `/Doctor/therapy`                        |
| Soft Delete Therapy           | DELETE `/Doctor/therapy?therapyId=`           |

## 🔐 Security & Validation

- Role based authorization (coarse controller boundaries).
- Input DTO validation (null / range) + semantic domain checks (e.g. measurement timing).
- Limited surface: no public self‑registration reduces attack area.
- Future: add policy‑based authorization & claims tightening.

## 🧪 Testing

xUnit project (`GlucoTrack_api.Tests`) covers controllers & critical domain logic (alert handling, validation). Roadmap includes contract tests & broader service isolation once service layer extracted.

Run tests:

```
dotnet test
```

## 🚀 Run (Development)

Prerequisites: .NET 8 SDK, SQL Server (or local container) configured.

1. Set connection string in `appsettings.Development.json`.
2. Apply schema: run SQL script `GlucoTrackDB_schema.sql` (or apply migrations when added).
3. Launch:

```
dotnet run
```

4. (Optional) Enable Swagger middleware for interactive docs at `/swagger`.

## 🧩 Design Notes

| Concern               | Decision                                                           |
| --------------------- | ------------------------------------------------------------------ |
| Transport Isolation   | DTO vs Entity mapping keeps contract stable if persistence evolves |
| Alert Processing      | Central logic prevents drift across controllers                    |
| Monitoring Separation | External task avoids heavy periodic jobs in request pipeline       |
| Future Refactor       | Extract dedicated service layer + repository if complexity grows   |

## 📈 Recruiter Snapshot

- Demonstrates clean, extensible Web API with clinically meaningful domain modeling.
- Applies layered validation & role security.
- Provides tangible extension points (monitoring, predictive, notifications).

## 📝 License

MIT (root repository applies).

---

This backend module is feature‑complete for the defined academic scope; roadmap items are optional enhancements.
