# 📅 Weekly Planner — Team Sprint Planning Tool

A full-stack web application that helps engineering teams plan and track their weekly sprint work. A **Team Lead** manages the planning cycle, and **team members** pick backlog items and commit hours each week.

**Live URLs**
- 🌐 Frontend: [weekly-planner-frontend-01.azurestaticapps.net](https://weekly-planner-frontend-01.azurestaticapps.net)
- 🔌 API: [weekly-planner-api-01.azurewebsites.net/api](https://weekly-planner-api-01.azurewebsites.net/api)

---

## ✨ Features

| Feature | Description |
|---|---|
| **Team Setup** | Add team members and designate a Team Lead |
| **Cycle Management** | Create and manage weekly cycles through 4 states |
| **Backlog** | Create, edit, archive backlog items by category |
| **Plan My Work** | Team members pick backlog items and commit hours (max 30h/week) |
| **Freeze & Review** | Lead reviews member plans and freezes the week |
| **Progress Tracking** | Members update task status and hours during the frozen phase |
| **Dashboard** | Category budget usage and per-member drill-down view |
| **Past Cycles** | View history of completed cycles |

---

## 🔄 Cycle States

```
SETUP ──▶ PLANNING ──▶ FROZEN ──▶ COMPLETED
                 └──▶ CANCELLED
```

| State | Who | What happens |
|---|---|---|
| `SETUP` | Lead | Configure week dates and hour allocations |
| `PLANNING` | Members | Pick backlog items, commit hours, mark ready |
| `FROZEN` | Members | Update progress (status + hours completed) |
| `COMPLETED` | Lead | Close the week |

---

## 🗂️ Backlog Categories

| Value | Display |
|---|---|
| `CLIENT_FOCUSED` | Client Focused |
| `TECH_DEBT` | Tech Debt |
| `R_AND_D` | R&D |

---

## 🛠️ Tech Stack

### Frontend
- **Angular 21** — standalone components, signals, `@if`/`@for` control flow
- **Angular Material** — UI components
- **TypeScript** — strict mode
- Deployed to **Azure Static Web Apps**

### Backend
- **ASP.NET Core (.NET 10)** — REST API
- **Entity Framework Core** — SQL Server provider
- **FluentValidation** — request validation
- **Azure SQL Database** — data store
- Deployed to **Azure App Service**

---

## 📁 Project Structure

```
weekly-plan-tracker/
├── frontend/
│   └── weekly-planner-frontend/       # Angular app
│       ├── src/app/
│       │   ├── core/services/         # HTTP services (cycle, auth, backlog…)
│       │   ├── features/              # Page components (home, planning, backlog…)
│       │   └── shared/                # Models, enums, dialogs, components
│       └── src/environments/          # Dev / prod environment configs
│
└── backend/
    ├── WeeklyPlanner.API/             # Controllers, Program.cs, appsettings
    ├── WeeklyPlanner.Core/            # DTOs, interfaces, domain services
    ├── WeeklyPlanner.Infrastructure/  # EF Core DbContext, repositories
    └── WeeklyPlanner.Tests/           # Unit tests
```

---

## 🚀 Running Locally

### Prerequisites
- Node.js **24.13.1** (or Node.js 20+)
- Angular CLI **21.2.0**
- .NET SDK **10.0.102**
- Azure SQL Database (or SQL Server)

### Backend

```bash
cd backend/WeeklyPlanner.API

# Set your connection string (do NOT commit credentials)
# In appsettings.Development.json:
# "DefaultConnection": "Server=...;Database=...;User ID=...;Password=...;"

dotnet run
# API available at: https://localhost:5102
```

### Frontend

```bash
cd frontend/weekly-planner-frontend

npm install
npm run start
# App available at: http://localhost:4200
```

---

## 🏗️ Production Build

### Frontend

```bash
cd frontend/weekly-planner-frontend
npm run build --configuration production
# Output: dist/weekly-planner-frontend/
```

### Backend

```bash
cd backend
dotnet publish WeeklyPlanner.API -c Release -o ./publish
```

---

## ☁️ Deployment

### Frontend → Azure Static Web Apps
Deploy the `dist/weekly-planner-frontend/browser/` folder.

### Backend → Azure App Service
1. Publish the .NET project
2. Configure the connection string in Azure Portal:
   > **App Service → Configuration → Connection Strings**
   > Name: `DefaultConnection` · Type: `SQLAzure`

### CORS
`appsettings.Production.json` is already configured to allow only the Azure Static Web App origin.

---

## 🔑 API Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/team-members` | List all team members |
| `POST` | `/api/team-members` | Add a team member |
| `GET` | `/api/cycles/active` | Get the current active cycle |
| `POST` | `/api/cycles/start` | Start a new cycle (Lead) |
| `PUT` | `/api/cycles/{id}/setup` | Configure cycle (Lead) |
| `PUT` | `/api/cycles/{id}/open` | Open planning (Lead) |
| `PUT` | `/api/cycles/{id}/freeze` | Freeze the plan (Lead) |
| `PUT` | `/api/cycles/{id}/complete` | Complete the cycle (Lead) |
| `GET` | `/api/backlog` | List backlog items |
| `POST` | `/api/backlog` | Create a backlog item |
| `PUT` | `/api/backlog/{id}` | Update a backlog item |
| `PUT` | `/api/backlog/{id}/archive` | Archive a backlog item |
| `POST` | `/api/assignments` | Claim a backlog item (Member) |
| `PUT` | `/api/assignments/{id}/progress` | Update task progress (Member) |
| `DELETE` | `/api/assignments/{id}` | Remove assignment (Member) |
| `PUT` | `/api/member-plans/{id}/ready` | Mark plan as ready (Member) |
| `GET` | `/api/cycles/{id}/progress` | Category progress for cycle |
| `GET` | `/api/cycles/{id}/members/{memberId}/progress` | Member progress |

---

## ⚙️ Configuration

### `appsettings.json` (base)
```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "<set via environment or appsettings.Development.json>"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://weekly-planner-frontend-01.azurestaticapps.net"
    ]
  }
}
```

### `appsettings.Production.json`
- CORS restricted to Azure Static Web App only
- Log level set to `Warning`
- Connection string set via **Azure App Service Configuration** (not in file)

---

## 🔒 Security Notes

- Never commit `appsettings.Development.json` with real credentials — add it to `.gitignore`
- Set the production connection string via Azure App Service → Configuration (not in source code)
- Rotate any credentials that were previously committed to git history

---

## 📄 License

MIT
