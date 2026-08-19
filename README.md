# KIPL Asset Management

An ASP.NET Core 8 Razor Pages application built to Clean Architecture, implementing the
KIPL asset management UI prototype as a working, database-backed system.

---

## Solution layout

```
KIPL.AssetManagement.sln
├── src
│   ├── KIPL.AssetManagement.Domain          entities, enums, invariants — no dependencies
│   ├── KIPL.AssetManagement.Application     use cases, DTOs, service + repository contracts
│   ├── KIPL.AssetManagement.Infrastructure  EF Core (SQL Server), Identity, seeding
│   └── KIPL.AssetManagement.Web             Razor Pages, authorization, the UI
└── tests
    └── KIPL.AssetManagement.UnitTests       xUnit + FluentAssertions + EF InMemory
```

Dependencies point inward only:

```
Web ──► Infrastructure ──► Application ──► Domain
 └──────────────────────────┘
```

`Domain` references nothing. `Application` talks to persistence through
`IApplicationDbContext` and to Identity through `IIdentityService`, so both are
substitutable and the use cases are testable without a database or a web host.

---

## Getting started

**Prerequisites** — .NET 8 SDK, and SQL Server (LocalDB, Express, or full).

```bash
cd KIPL.AssetManagement
dotnet restore
dotnet build
```

Create the database. The app applies migrations on startup, but the migration has to
be generated once first. Use **either** of the following — not both.

#### Option A — Visual Studio, Package Manager Console

`Tools ▸ NuGet Package Manager ▸ Package Manager Console`. Set **Default project** to
`src\KIPL.AssetManagement.Infrastructure`, then run this as a *single line*:

```powershell
Add-Migration InitialCreate -Project KIPL.AssetManagement.Infrastructure -StartupProject KIPL.AssetManagement.Web
```

These are PowerShell cmdlets, so use `Add-Migration`, not `dotnet ef migrations add`.
Project names here are the *project* names, without the `src/` folder prefix.

To apply it immediately (optional — startup does this anyway):

```powershell
Update-Database -StartupProject KIPL.AssetManagement.Web
```

#### Option B — command line

From the `KIPL.AssetManagement` folder, in a normal terminal (**not** the Package
Manager Console):

```bash
dotnet tool install --global dotnet-ef      # first time only
dotnet ef migrations add InitialCreate --project src/KIPL.AssetManagement.Infrastructure --startup-project src/KIPL.AssetManagement.Web
```

### Run it

Press **F5** in Visual Studio, or:

```bash
dotnet run --project src/KIPL.AssetManagement.Web
```

Browse to <https://localhost:7218>. On first start the database is created and seeded
with the reference data from the prototype — departments, people, assets, requests,
returns and audit history.

### Connection string

`src/KIPL.AssetManagement.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=KIPL;Database=KIPL_AssetManagement;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=True"
}
```

This points at a named SQL Server instance called `KIPL` using Windows Authentication (no username/password stored — the app connects as whoever is logged into Windows). If you're back on LocalDB instead, swap `Server=KIPL` for `Server=(localdb)\MSSQLLocalDB`.

Set `"SeedData:Enabled": false` to skip seeding once you have real data.

---

## Demo accounts

All seeded accounts share the password **`Kipl@12345`**.

| Role | Email |
|---|---|
| Admin | `admin@kipl.com` |
| IT Asset Manager | `vikram.singh@kipl.com` |
| IT Asset Executive | `revanth.k@kipl.com` |
| HR | `amanda.lee@kipl.com` |
| Reporting Manager | `michael.chen@kipl.com` |
| Employee | `priya.shankar@kipl.com` |

The login page lists these and fills the form when you click one.

---

## Authorization model

Two layers, because roles alone were too blunt for this domain:

1. **Roles** — the six above, held in ASP.NET Core Identity.
2. **Permissions** — fine-grained claims (`inventory.manage`, `requests.approve`, …)
   granted to roles through the `RolePermissions` table.

The Roles &amp; Permissions screen edits that table; changes are written back to Identity
role claims, which are stamped into the auth cookie at sign-in. Pages declare what they
need (`[Authorize(Policy = Permissions.ManageUsers)]`) and `PermissionPolicyProvider`
builds the matching policy on demand.

Administrators bypass the matrix entirely, so the system cannot lock itself out.

---

## Domain flows

**Request** — an employee raises it → their reporting manager (or HR) approves →
it becomes *Unclaimed* → an ops user claims it → they either match it to stock and
dispatch it, or place an online order that HR verifies on delivery → the employee
confirms receipt, and only then does the asset become *Assigned*.

**Return** — the employee raises it against an asset they hold → hands it over
(courier or office desk) → IT marks it received → inspection decides the outcome:
good goes back to stock, damaged goes to service, beyond repair is retired.

State transitions live on the entities (`AssetRequest.Approve`, `AssetReturn.Inspect`,
`Asset.AssignTo`), so an illegal move throws `DomainException` no matter which
service or page attempts it.

---

## Screens

| Area | Pages |
|---|---|
| Operations | Dashboard (tabbed &amp; sortable), Inventory, Asset details/edit, Return tracking, Service queue, Bulk import, Delivery challan |
| Approvals | HR approvals, Executive fulfilment queue, Team approvals |
| Administration | Users, User edit, Roles &amp; permissions, Audit trail |
| Self-service | My dashboard, My assets, My requests, My returns |

The left rail is composed per role in `Web/Infrastructure/Navigation.cs`, matching the
prototype's `roleConfig` ordering and labels.

### Interaction model

The UI follows the prototype: actions open **modal dialogs** rather than navigating away.
`site.js` provides `openModal`/`closeModal`, and any button carrying `data-modal="name"`
opens `#modal-name`, copying its `data-*` attributes into matching `[data-field]` and
`[data-input]` elements inside. That is what lets a single dialog serve every row of a
table without a round trip.

Dialogs post as ordinary forms to named page handlers, so nothing depends on fetch or
JSON. **Edit asset** and **edit user** are modals too, rendered once per table row
(each with a unique `id="modal-editAsset-{id}"` / `id="modal-editUser-{id}"`) and backed
by a batch-loaded detail dictionary, so every field survives the round trip without
blanking the rest of the record on save.

Some flows need a server round trip before the right modal can open — dispatching a
request generates a delivery challan, for example. For those, the page sets
`TempData["OpenModal"]` before redirecting, and `#toastStack` in the layout carries it
as `data-open-modal` so `site.js` calls `openModal(...)` once the page has reloaded.

Toasts are raised client-side from `TempData` via the `#toastStack` element in the layout.

### Photo capture

Photos are captured at assignment, dispatch, proof of receipt, return handover, return
inspection and online-order verification. Uploads are validated (JPEG/PNG/WebP/HEIC,
8 MB cap), stored through `IFileStorage`, and recorded as `AssetPhoto` rows linked to the
asset, request or return.

`LocalFileStorage` writes under `Storage:UploadRoot` (defaulting to `wwwroot/uploads`),
foldered by kind and year/month, renaming each file to a GUID so a hostile filename
cannot escape the root. Swapping to blob storage means replacing that one class.

### Delivery challan

Dispatching from stock generates a GST-style delivery challan (Rule 55, CGST Rules 2017)
listing every asset in the batch — several items ticked in the assign dialog ship on one
challan. The print stylesheet hides everything except the sheet, so `Ctrl+P` yields a
clean A4 document. Asset tag labels print the same way.

### Online order route

When nothing is in stock, operations logs an order instead. The item ships direct to the
employee, and HR checks it in on arrival: the serial number is mandatory, a declared
mismatch must be explained, and only then does the item become a real inventory asset.

### Self-approval

A request raised by someone who is themselves an approver shows as **NEEDS ADMIN** and
cannot be actioned at that level — the service rejects self-approval regardless of which
screen it is attempted from.

---

## Tests

```bash
dotnet test
```

Covers the domain invariants (assignment rules, request and return lifecycles) and the
application services (inventory, requests, returns, CSV bulk import) against an
in-memory provider.

---

## Notes on the UI

The prototype's stylesheet is used verbatim in `wwwroot/css/site.css`. `app.css` holds
only the overrides needed to move from a single-file prototype that toggled `.page`
visibility with JavaScript to server-rendered routes.

Every screen works with JavaScript disabled — `site.js` is progressive enhancement
(inline expansion panels, filter auto-submit, confirmation prompts).

---

## Audit trail

Every state change appends an `AuditEntry` in the same `SaveChanges` call as the change
itself, so the trail cannot drift from the data. The Audit screen filters by user, role,
action and date, and exports to CSV.
