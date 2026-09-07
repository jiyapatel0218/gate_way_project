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

## Payments (Razorpay)

The payment gateway is config-switchable between `Mock` (always succeeds, no external calls — the default) and `Razorpay`, set via `PaymentGateway:Provider` in `appsettings.json`.

### Getting your Razorpay Key ID / Key Secret

1. Sign up / log in at the [Razorpay Dashboard](https://dashboard.razorpay.com/).
2. Go to **Settings → API Keys**.
3. Click **Generate Test Key** for development, or **Generate Live Key** for production (live mode requires KYC/business verification to be completed first).
4. Razorpay shows the **Key Secret only once** — copy both values immediately. If you lose the secret, you must regenerate the key pair.
5. Test-mode keys are prefixed `rzp_test_...`, live keys `rzp_live_...`. Use test keys during development; Razorpay's [test card/UPI numbers](https://razorpay.com/docs/payments/payments/test-card-upi-details/) let you exercise success and failure paths without moving real money.

### Where to configure them

Credentials are **never hard-coded** — `RazorpayPaymentGatewayService` reads them from `IConfiguration` at `PaymentGateway:Razorpay:KeyId` / `KeySecret` / `WebhookSecret`, which are empty placeholders in the committed `appsettings.json`.

- **Development**: copy `SocietyGatekeeper.API/appsettings.Local.json.example` to `appsettings.Local.json` (gitignored) and fill in your test keys — see [Setup](#1-backend). Then set `PaymentGateway:Provider` to `Razorpay` in `appsettings.json`.
- **Production**: don't ship a `Local.json` file to a server. Set the equivalent environment variables instead (ASP.NET Core's configuration binder maps `__` to nested keys):
  ```
  PaymentGateway__Razorpay__KeyId=rzp_live_...
  PaymentGateway__Razorpay__KeySecret=...
  PaymentGateway__Razorpay__WebhookSecret=...
  PaymentGateway__Provider=Razorpay
  ```
  or your platform's secret manager (Azure Key Vault, AWS Secrets Manager, etc.) wired in as an additional configuration provider in `Program.cs`.

### Payment flow

1. **Create order** — `POST /api/maintenance/{invoiceId}/payments/online/create-order` calls Razorpay's Orders API and returns an order id + the public Key ID (never the secret) to the browser.
2. **Checkout** — the frontend loads `checkout.razorpay.com/v1/checkout.js` and opens Razorpay's hosted checkout with that order id.
3. **Verify** — on success, Razorpay's `handler` callback returns a payment id + signature to the browser, which posts them to `POST /api/maintenance/payments/online/verify`. The backend recomputes `HMAC-SHA256("{order_id}|{payment_id}", key_secret)` and compares it against the signature before marking the invoice paid — this is what makes the result trustworthy rather than just believing the client.
4. **Webhook (fallback)** — `POST /api/maintenance/payments/online/webhook` handles the case where a payment succeeds at Razorpay but the browser never completes step 3 (closed tab, dropped connection). It verifies the `X-Razorpay-Signature` header against the raw request body using `PaymentGateway:Razorpay:WebhookSecret`, and is idempotent with step 3 (whichever arrives first marks the order paid; the other becomes a no-op). To enable it:
   - In the Razorpay Dashboard, go to **Settings → Webhooks → Add New Webhook**.
   - URL: `https://<your-domain>/api/maintenance/payments/online/webhook`.
   - Active event: `payment.captured`.
   - Razorpay generates a webhook secret at that point — put it in `PaymentGateway:Razorpay:WebhookSecret`. It's a separate value from the Key Secret.
   - The webhook is a no-op (`404`) until `WebhookSecret` is configured, so it's safe to leave unset in development.
5. **Failure handling** — if signature verification fails, the order is marked `Failed` and the invoice stays unpaid; the resident sees an error and can retry, which creates a fresh order.

## Notes

- **OTP delivery (Forgot Password) is not connected to a real email/SMS provider** — verification codes are returned directly in the API response and shown on-screen (`devOtp`) for local testing. Wire in a real SMTP or SMS provider before relying on this in production.
