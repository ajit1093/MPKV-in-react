# MPKV Candidate Portal — React + .NET Core

## Project Structure
```
MpkvCandidate/
├── backend/
│   └── MpkvCandidate.Api/      ← ASP.NET Core 8 Web API
└── frontend/                   ← React 18 + Tailwind CSS
```

---

## How to Run

### Step 1 — Backend

Open a terminal in `backend/MpkvCandidate.Api` and run:

```
dotnet restore
dotnet run
```

API will start at: https://localhost:7000  
Swagger UI will open at: https://localhost:7000/swagger

### Step 2 — Frontend

Open a terminal in `frontend` and run:

```
npm install
npm run dev
```

React app will start at: http://localhost:5173

---

## First Time Setup

1. Make sure SQL Server is running with database `2026_MPKV_Rahuri_Test`
2. Check connection string in `backend/MpkvCandidate.Api/appsettings.json`
3. If your SQL Server uses SQL auth (username/password), change the connection string to:
   ```
   Server=localhost;Database=2026_MPKV_Rahuri_Test;User Id=sa;Password=yourpassword;TrustServerCertificate=True;
   ```

---

## API Endpoints

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | /api/auth/login | No | Candidate login — returns JWT token |
| GET | /api/auth/me | Yes | Returns current logged-in user info |
| GET | /api/dashboard | Yes | Full dashboard data + progress |
| GET | /api/dashboard/progress | Yes | Progress stepper data only |

---

## How Auth Works

1. Call `POST /api/auth/login` with `{ userLoginID, userPassword }`
2. On success, you get back a `token` (JWT) and `user` object
3. React stores the token in `localStorage`
4. Every API call after that sends `Authorization: Bearer <token>` in the header
5. Token expires after 8 hours — user is redirected to login automatically
