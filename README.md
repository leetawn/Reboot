# Reboot

Reboot is a peer-to-peer web marketplace for pre-owned electronics, gaming hardware, and computer components. Designed with a clean, editorial aesthetic, Reboot replaces the uncertainty of traditional classified sites with structured diagnostic checklists, calculated condition grading, platform escrow holding, and transparent seller reliability facts.

---

## Key Features & Escrow State Machine

1. **Structured Condition Checklists & Calculated Grading**
   - Sellers complete diagnostic checklists tailored to each category (e.g., HDMI 2.1 outputs, stick drift circularity, thermal throttling under load, battery health).
   - Grades (**Grade A**, **Grade B**, **Grade C**, **For Parts**) are computed deterministically by platform rules from diagnostic checks and disclosed defects—never typed freely by sellers.

2. **Platform Escrow State Machine**
   - `Reserved` (24h countdown): Buyer initiates order; listing locks atomically.
   - `Paid (Held)`: Buyer simulates payment; funds are locked securely in platform escrow.
   - `Handed Over / Shipped` (48h inspection countdown): Seller hands over or dispatches item.
   - `Confirmed -> Released`: Buyer tests hardware against the condition report and confirms receipt, immediately releasing payout to the seller.
   - `Disputed -> Resolved`: Buyer can open a dispute during inspection, freezing funds for administrative review (Full Refund, Release, or Partial Settlement).

3. **Public Reliability Records**
   - Seller reliability records display plain facts (completed sales, dispute rates, average response time, account age).
   - Recency weighting applies a 2x factor to transactions in the last 60 days.
   - Circular wash-trading filters ignore reciprocal feedback between accounts that only trade with each other.

4. **Identity Verification Barrier**
   - Unverified users may browse and buy, but must complete identity verification before listing hardware for sale.

---

## Authentication & Test Credentials

Reboot uses real ASP.NET Core cookie authentication (`CookieAuthenticationDefaults.AuthenticationScheme`) with a 14-day sliding expiration and PBKDF2 SHA-256 password hashing (100,000 iterations). Accounts automatically lock out for 15 minutes after 5 consecutive failed attempts.

All pre-seeded demo accounts share the standard password:
```
Password123!
```

### Pre-Seeded Accounts

| Role | Name | Email | Default Password | Notes |
|---|---|---|---|---|
| **Administrator** | Marco T. | `admin@reboot.ph` | `Password123!` | Access to `/admin` dispute arbitration & verification |
| **Top Seller** | Dave A. | `dave@reboot.ph` | `Password123!` | Verified seller with 14 completed sales |
| **Active Seller/Buyer** | Kirsten M. | `kirsten@reboot.ph` | `Password123!` | Verified account with active handheld listings |
| **Seller** | Ramon V. | `ramon@reboot.ph` | `Password123!` | Verified account with console listings |
| **Buyer** | Paolo G. | `paolo@reboot.ph` | `Password123!` | Verified active buyer persona |
| **Unverified User** | Bea C. | `bea@reboot.ph` | `Password123!` | Unverified account for testing the seller verification gate |

New users can also be registered at `/register` using any Metro Cebu municipality.

---

## Routes & Application Sitemap

| Route | Page | Description |
|---|---|---|
| `/` | **Landing Page** | Platform introduction, live computed platform statistics, 3-step escrow walkthrough, reliability comparison, 6 recent listings, grading criteria table, and conditional seller CTA. |
| `/browse` | **Browse Catalog** | Full inventory grid with keyword search, category pills, grade dropdown, price range, Cebu location filters, verified-seller filter, warranty filter, and sorting. |
| `/listing/{id}` | **Listing Detail** | Hardware specs, serial number, complete diagnostic condition table, disclosed defects, seller reliability facts, Buy Now (escrow), and Make an Offer. |
| `/sell` | **Sell Gear** | Step-by-step listing creation with verification barrier, category-specific diagnostic checks, live calculated grade preview, and warranty guarantees. |
| `/my/listings` | **Seller Dashboard** | Inventory table by status (Active, Reserved, Sold, Removed), incoming offer actions (Accept/Decline), and hand-over action alerts. |
| `/my/orders` | **Orders & Escrow** | Unified buyer/seller orders with 5-step horizontal escrow indicator, live countdown timers, state actions (Pay, Hand Over, Confirm, Dispute), and verified reviews. |
| `/seller/{id}` | **Seller Profile** | Plain-facts seller reliability card, active listings, and verified reviews. |
| `/login` | **Sign In** | Google/YouTube-style centered card sign-in supporting `returnUrl` and 14-day persistent session cookies. |
| `/register` | **Sign Up** | Clean centered card registration with Metro Cebu locations, password validation, and terms agreement. |
| `/forgot-password` | **Forgot Password** | Account recovery with email submission and reset token generation. |
| `/reset-password` | **Reset Password** | Secure password reset with token verification. |
| `/terms` | **Terms of Service** | Marketplace terms and condition disclosure rules. |
| `/privacy` | **Privacy Policy** | Data privacy and cookie authentication disclosures. |
| `/admin` | **Administration** | Open dispute arbitration (Refund, Release, Partial Settlement), user identity verification queue, and listing moderation. |

---

## Technology Stack

- **Framework**: Blazor Web App (.NET 8) with Interactive Server render mode.
- **Styling**: Tailwind CSS standalone CLI (`v4.3.3`). No Node/npm is needed.
- **Design Tokens**: Defined in `Styles/input.css` using `@theme` and `@source`.
- **Typeface**: **Schibsted Grotesk** (weights 400, 500, 600) and **JetBrains Mono** (weight 400).
  - Self-hosted in `wwwroot/fonts/` (WOFF2 format).
  - Loaded via `@font-face` with `font-display: swap` in `Styles/input.css`.
  - Zero external font CDN requests.
  - Licensed under the [SIL Open Font License (OFL)](https://openfontlicense.org/).
  - Monospace / Tabular numerals (`font-mono`, `font-variant-numeric: tabular-nums`) applied strictly on prices, serial numbers, order IDs, and countdown timers.

---

## Running the Application Locally

### 1. Run the .NET Server
```powershell
dotnet run --launch-profile http
```
The application will start and listen at **`http://localhost:5088`**.

### 2. Build or Watch Tailwind CSS (Standalone CLI)
The project automatically compiles Tailwind CSS during `dotnet build` via an MSBuild target in `Reboot.csproj`.

To manually compile or watch for changes during styling:
```powershell
# Single build & minify
.\tailwindcss.exe -i Styles\input.css -o wwwroot\app.css --minify

# Watch mode
.\tailwindcss.exe -i Styles\input.css -o wwwroot\app.css --watch
```

---

## Simulated vs Real Systems

- **Real Implementations**:
  - ASP.NET Core cookie authentication handler (`CookieAuthenticationDefaults.AuthenticationScheme`) with 14-day persistent sliding expiration.
  - PBKDF2 SHA-256 password hashing (100,000 iterations) with salt.
  - 5-attempt/15-minute account lockout security.
  - Complete escrow finite state machine with real timer countdowns.
  - Automatic deterministic grade derivation from 15+ diagnostic checklist tests.
  - Weighted seller reputation score engine with circular wash-trading filters.
  - Real minimal API endpoints for login, registration, password reset, and logout.

- **Simulated Implementations**:
  - Payment gateway APIs (credit card, GCash, Maya) simulate instant authorization without actual banking fees.
  - Courier logistics (LBC, Grab, Maxim) simulate hand-off transitions.
  - Government ID photo upload simulates document verification.
  - Database persistence uses a thread-safe singleton in-memory data store with realistic seed data.
