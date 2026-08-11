# Database changes — Structured Actions (PDCA), phase 1

## Do you need to run anything by hand?

**Normally no.** The app applies this automatically on startup:

- `db.Database.Migrate()` creates the new **`AuditActions`** table (migration
  `20260811101748_AddAuditActions`). It's a brand-new table, so there's no
  clash with anything existing.
- The new **`ActionOwner`** picker list is seeded automatically (TL + HOD +
  Senior + full-access names, plus the shared destination **Maintenance**) by
  the same self-sync that handles the other name lists — no SQL needed.

Run the scripts below only if you apply schema by hand rather than letting the
app migrate, or to verify/seed on a locked-down DB.

## 1. Create the table (idempotent)

Run **`deploy/add-audit-actions.sql`** in SSMS. It is guarded on
`__EFMigrationsHistory`, so it creates `AuditActions` only if the migration
hasn't been applied, and stamps history so EF stays consistent. Safe to re-run.

The table (all free text is `NVARCHAR(MAX)` to avoid truncation):

| Column | Type |
|---|---|
| Id | int identity PK |
| SourceType | nvarchar(40) |
| SourceId | int null |
| SourceLabel | nvarchar(max) null |
| AuditType | nvarchar(120) null |
| Area | nvarchar(200) null |
| AuditDate | date null |
| Text | nvarchar(max) |
| RaisedByName / RaisedByUsername | nvarchar(256) |
| RaisedAt | datetime2 |
| OwnerName / OwnerKey | nvarchar(256) |
| OwnerIsExternal | bit |
| DueDate | date null |
| Status | nvarchar(20) |
| CompletedAt | datetime2 null |
| CompletedByName | nvarchar(256) null |
| CompletionNote | nvarchar(max) null |

Indexes: `(Status, OwnerKey)` and `(SourceType, SourceId)`.

## 2. Seed action owners (only if the app can't self-seed)

The app seeds these on startup. To do it manually — idempotent:

```sql
-- Shared external destination(s)
IF NOT EXISTS (SELECT 1 FROM dbo.PickerPersons WHERE ListKind = N'ActionOwner' AND LOWER(Name) = LOWER(N'Maintenance'))
    INSERT INTO dbo.PickerPersons (ListKind, Name, SortOrder)
    SELECT N'ActionOwner', N'Maintenance', ISNULL(MAX(SortOrder),0)+1 FROM dbo.PickerPersons WHERE ListKind = N'ActionOwner';

-- Individual owners: copy every existing TL / HOD / Senior / FullAccess name into ActionOwner
INSERT INTO dbo.PickerPersons (ListKind, Name, SortOrder)
SELECT N'ActionOwner', s.Name,
       ROW_NUMBER() OVER (ORDER BY s.Name) + (SELECT ISNULL(MAX(SortOrder),0) FROM dbo.PickerPersons WHERE ListKind = N'ActionOwner')
FROM (SELECT DISTINCT Name FROM dbo.PickerPersons
      WHERE ListKind IN (N'TeamLeader', N'Hod', N'Senior', N'FullAccess')) s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PickerPersons p
    WHERE p.ListKind = N'ActionOwner' AND LOWER(p.Name) = LOWER(s.Name));

SELECT Name FROM dbo.PickerPersons WHERE ListKind = N'ActionOwner' ORDER BY SortOrder;
```

## What is NOT changed

`ActionsRaised` on the audit tables is untouched — structured actions live in the
new `AuditActions` table alongside it. No existing table, column, or data is
altered.
