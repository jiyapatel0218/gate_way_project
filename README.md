# Smart Society Gatekeeper Management System

A full-stack management platform for residential societies/gated communities — resident and staff management, visitor logging with real-time approval, complaints, maintenance billing with online payments and PDF invoices, property listings, notices, and more, with role-based access for four distinct roles.

## Roles

| Role | Access |
|---|---|
| 👑 Super Admin | Full system access across every society |
| 🏢 Society Secretary (Society Admin) | Manage residents, staff, and operations for their own society |
| 🏠 Resident | Pay maintenance, raise complaints, invite guests, view notices |
| 🛡️ Security Guard | Log visitors and manage gate entries |

## Tech Stack

**Backend** — ASP.NET Core 8 Web API, EF Core 8 + SQL Server, ASP.NET Identity + JWT auth, SignalR (real-time notifications), Hangfire (scheduled jobs), QuestPDF (invoice PDFs), ClosedXML (Excel export).

**Frontend** — Angular 21 (standalone components, zoneless change detection, signals), Angular Material, Tailwind CSS, GSAP.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18+) and npm
- SQL Server (Express edition is fine) with a local `SQLEXPRESS` instance, or update the connection string to point at your own instance
- Angular CLI: `npm install -g @angular/cli`

## Setup

### 1. Backend

```bash
cd backend

# Copy the local secrets template and fill in your own random values
cp SocietyGatekeeper.API/appsettings.Local.json.example SocietyGatekeeper.API/appsettings.Local.json
# edit appsettings.Local.json — set Jwt:Key and Otp:Pepper to long random strings

dotnet restore
dotnet run --project SocietyGatekeeper.API
```

The API applies EF Core migrations and seeds demo data automatically on first run. It listens on **http://localhost:5250** (Swagger UI available at `/swagger` in development).

If your SQL Server instance isn't a local `SQLEXPRESS` with Windows auth, update `ConnectionStrings:DefaultConnection` in `backend/SocietyGatekeeper.API/appsettings.json`.

### 2. Frontend

```bash
cd frontend
npm install
ng serve
```

Open **http://localhost:4200** — this redirects to the role-selection landing page.

### Demo credentials (seeded automatically)

| Role | Email | Password |
|---|---|---|
| Super Admin | `superadmin@greengate.com` | `SuperAdmin@123` |
| Society Secretary | `admin@greengate.com` | `Admin@123` |
| Resident | `resident@greengate.com` | `Resident@123` |
| Security Guard | `guard@greengate.com` | `Guard@123` |

> Change or remove the Super Admin demo password before deploying anywhere beyond local development.

## Project Structure

```
backend/
  SocietyGatekeeper.Domain/         entities, enums
  SocietyGatekeeper.Application/    DTOs, interfaces, validation
  SocietyGatekeeper.Infrastructure/ EF Core DbContext, Identity, JWT, SignalR, Hangfire jobs
  SocietyGatekeeper.API/            controllers, Program.cs, seed data
frontend/
  src/app/core/     services, guards, models shared across features
  src/app/features/ one folder per screen/module (auth, residents, maintenance, ...)
  src/app/layout/   app shell (nav, header)
database/           idempotent SQL schema scripts (regenerate via `dotnet ef migrations script`)
```

## Notes

- **OTP delivery (Forgot Password) is not connected to a real email/SMS provider** — verification codes are returned directly in the API response and shown on-screen (`devOtp`) for local testing. Wire in a real SMTP or SMS provider before relying on this in production.
- The online payment gateway defaults to a `Mock` provider (`PaymentGateway:Provider` in `appsettings.json`) that always succeeds — switch to `Razorpay` and supply real keys to go live.
