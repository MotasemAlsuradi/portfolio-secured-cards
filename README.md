# Portfolio Secured Cards

An **Investment-Backed Credit Card module** built with **.NET 8**, **Angular**, and **Clean Architecture**.

Customers obtain credit cards secured by liens on their investment portfolios. The module enforces lien management, credit-limit rules, redemption restrictions in the Domain layer, and automated delinquency monitoring through Hangfire.

> **Scope:** This is a simplified simulation of a secured credit product, not a production banking system.

---

## Technology Stack

**Backend**

- ASP.NET Core Web API (.NET 8)
- Clean Architecture (Domain, Application, Infrastructure, API)
- CQRS with MediatR
- Entity Framework Core InMemory
- Hangfire with `Hangfire.InMemory` storage for delinquency monitoring
- xUnit for automated backend testing

**Frontend**

- Angular (module-based using NgModules; no standalone components)
- Angular Reactive Forms for business operations
- TypeScript, HTML, CSS

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) and npm
- Angular CLI does **not** need to be installed globally; the commands below use `npx`.

---

## Getting Started

Run the commands from the **repository root**, unless otherwise specified.

### 1. Run the Backend

```powershell
cd src/backend

# Trust the local HTTPS development certificate (first time only)
dotnet dev-certs https --trust

# Restore and run
dotnet restore PortfolioSecuredCards.sln
dotnet run --project PortfolioSecuredCards.Api --launch-profile https
```

The development API runs at `https://localhost:7168`.

| Resource | URL |
| --- | --- |
| Swagger UI | `https://localhost:7168/swagger` |
| Hangfire Dashboard | `https://localhost:7168/hangfire` (Development only; local requests only) |

### 2. Run the Frontend

From the repository root, open a **second terminal**:

```powershell
cd src/frontend/portfolio-secured-cards-ui
npm install
npx ng serve --proxy-config proxy.conf.json
```

Open `http://localhost:4200`.

> **Windows PowerShell:** If script execution policies block npm or npx, use `npm.cmd` and `npx.cmd` instead.

### API Connectivity & CORS

- The Angular development proxy forwards `/api` requests to `https://localhost:7168` by default.
- The backend also registers and enables an `AngularClient` CORS policy allowing **direct cross-origin browser requests** from `http://localhost:4200`.
- That policy permits HTTP methods and request headers, including `X-Demo-Customer-Id`. It does **not** permit arbitrary origins.
- The development proxy remains the default frontend setup; no Angular configuration change is needed.
- CORS is a browser access policy, **not** authentication or authorization.

### 3. Run the Backend Tests

From the repository root:

```powershell
cd src/backend
dotnet test PortfolioSecuredCards.sln
```

---

## Architecture

### Backend — Clean Architecture

```text
API ───────────────► Application ───────► Domain
 │                         ▲
 └─────► Infrastructure ───┘
```

| Layer | Responsibility |
| --- | --- |
| **Domain** | Aggregates (`Portfolio` with its `Lien`s, `CreditCard`), business invariants, and domain exceptions. No framework dependencies. |
| **Application** | CQRS commands, queries, and handlers; cross-aggregate orchestration; persistence and time abstractions. |
| **Infrastructure** | `AppDbContext` (EF Core InMemory), repositories, read services, seed data, and background-job infrastructure. |
| **API** | Controllers, request/response contracts, global exception handling, dependency injection, Swagger, and CORS configuration. |

### CQRS

- **Commands** load aggregates through repositories, invoke domain behavior, and persist changes through `IUnitOfWork`.
- **Queries** are read-only and return DTOs; credit-card numbers are masked in responses.
- Controllers delegate application operations through MediatR rather than implementing business rules.

### Aggregates

- **Portfolio** is an aggregate root that owns its **Liens** and enforces `Σ(unreleased liens) ≤ TotalValue` and redemption rules.
- **CreditCard** is a separate aggregate root referencing its portfolio and lien by ID.
- Rules involving both aggregates (such as restricting usage on a frozen portfolio or releasing a lien on card closure) are coordinated in the Application layer.

### Frontend

The Angular application uses a feature module (`SecuredCardsModule`) containing the customer-selection view, investment-portfolio list, and credit-card list. Shared services handle API communication and customer context, while an HTTP interceptor adds `X-Demo-Customer-Id` to customer-scoped requests.

**Business operation forms** use Angular Reactive Forms. **Customer selection** uses a native HTML `<select>` with a change event. Delinquency monitoring is a system-wide action and does not require a selected customer.

---

## Features

### Investment Portfolios

- View a customer's **Basic**, **Plus**, and **Premium** portfolios.
- Display total value, liened amount, available value, and status.
- Redeem available (non-liened) value.
- Reject redemption that violates available-value or portfolio-status rules.

### Secured Credit Cards

- Issue a card secured by a dedicated lien on the selected portfolio.
- Enforce `0 < CreditLimit ≤ LienAmount`.
- Simulate card usage and reject amounts exceeding available credit.
- Record successful usage with the amount, utilization before and after, and timestamp.
- Display card usage history.
- Repay utilized amounts.
- Close fully repaid cards and release their associated liens.

### Delinquency Monitoring (Hangfire)

- A recurring Hangfire job executes **hourly** and scans fully utilized cards.
- Cards that remain fully utilized beyond the configured threshold are marked **Delinquent**.
- Associated portfolios are **Frozen**, blocking further card usage on the frozen portfolio.
- A manual **Run Delinquency Scan** action triggers a system-wide scan without requiring customer selection.
- Scanning is designed to be **idempotent**: previously processed delinquent cards are not newly marked delinquent again.
- The threshold is configured using `Delinquency:ThresholdMinutes`: **24 hours by default** and **1 minute in Development** for demonstration.

### Error Handling & API Responses

The API uses ASP.NET Core `IExceptionHandler` to handle exceptions centrally and return standard `ProblemDetails` responses containing the HTTP status, title, detail, and request path.

- **Domain exceptions** include an application-specific `errorCode` in the `ProblemDetails` response.
- **`ArgumentException`** returns `400 Bad Request`.
- **`KeyNotFoundException`** returns `404 Not Found`.
- **Unexpected exceptions** return `500 Internal Server Error` with a generic message, without exposing internal exception details.
- The handler adds `errorCode` **only** for `DomainException`. Other exception types do not automatically include that field.

#### Domain error codes mapped to `409 Conflict`

The following domain error codes are explicitly mapped to `409 Conflict` by `GlobalExceptionHandler`:

| Error code | Meaning |
| --- | --- |
| `PortfolioFrozen` | Operation is not allowed on a frozen portfolio |
| `InsufficientAvailableValue` | A lien or redemption exceeds the portfolio's available value |
| `CreditLimitExceeded` | Card usage exceeds available credit |
| `CardNotActive` | An operation requires an active card |
| `OutstandingBalance` | A card cannot be closed while utilization remains outstanding |
| `CardAlreadyClosed` | The card has already been closed |
| `LienAlreadyReleased` | The associated lien has already been released |

**All other `DomainException` error codes return `400 Bad Request`** under the current exception-handler mapping. For example, `CreditLimitExceedsLien` is **not** mapped to `409`; if emitted as a `DomainException`, it returns `400`.

Resource-not-found responses return `404` when raised as `KeyNotFoundException`. In that case, the response does not automatically include an `errorCode`. The exact error code and HTTP status for any individual operation depend on the exception type raised by its handler.

---

## Design Decisions (Beyond the Assignment)

These additional decisions complete the simulated card lifecycle while keeping its behavior consistent:

- **Editable credit limit:** The issue form defaults the credit limit to the lien amount and permits a lower value, subject to `0 < CreditLimit ≤ LienAmount`.
- **One lien per card:** Every issued card has a dedicated portfolio lien.
- **Repay and close:** Repayment reduces utilization but **does not release the lien**. The lien is released only when the card is **closed with zero utilized balance**.
- **Frozen portfolio restrictions:** Frozen portfolios reject redemption, new card issuance, and card usage, while permitting repayment and closure.
- **Unfreeze:** A portfolio returns to **Active** after its **last delinquent card** is closed.
- **Customer ownership:** Customer-scoped operations verify that the requested portfolio or card belongs to the selected customer.

---

## Demo Data

Sample customers and investment portfolios are seeded into EF Core InMemory at application startup:

- Ahmad Ali
- Omar Khaled
- Sara Hassan

Select a customer in Angular. For this demo, the frontend passes the selected customer ID in an `X-Demo-Customer-Id` HTTP header instead of using real authentication.

---

## API Endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/customers` | Retrieve demo customers |
| GET | `/api/portfolios` | Retrieve customer portfolios |
| POST | `/api/portfolios/{portfolioId}/redeem` | Redeem available portfolio value |
| GET | `/api/credit-cards` | Retrieve customer credit cards |
| GET | `/api/credit-cards/{cardId}` | Retrieve card details |
| POST | `/api/credit-cards` | Reserve a lien and issue a card |
| POST | `/api/credit-cards/{cardId}/use` | Record card usage |
| POST | `/api/credit-cards/{cardId}/repay` | Repay utilized amount |
| POST | `/api/credit-cards/{cardId}/close` | Close a card and release its lien |
| GET | `/api/credit-cards/{cardId}/usage-history` | Retrieve card usage history |
| POST | `/api/delinquency/scan` | Trigger a system-wide manual delinquency scan |

All endpoints except `/api/customers` and `/api/delinquency/scan` use the `X-Demo-Customer-Id` header for demo customer context.

---

## Demo Workflow

1. **Select a customer** in the Angular interface.
2. Review the customer's portfolios, available values, and existing liens.
3. **Issue a secured credit card** and observe the increased liened amount.
4. **Use the card** and review its **Usage History**.
5. Fully utilize the card until `AvailableCredit = 0`.
6. After the configured delinquency period has elapsed (**1 minute in Development**), click **Run Delinquency Scan**.
7. Verify that the card becomes **Delinquent** and its portfolio becomes **Frozen**.
8. Attempt to redeem from the frozen portfolio and verify that the request is rejected.
9. **Repay** the full utilized balance; the associated lien remains reserved.
10. **Close** the fully repaid card; its lien is released.
11. Verify that the portfolio returns to **Active** if no other delinquent cards remain and that the liened and available values are updated.

> The manual delinquency scan is **system-wide**. The button is available even when no customer is selected; if a customer is selected, the visible portfolio and card lists are refreshed after the scan.

---

## Testing

**All tests executed for this project have passed.** Automated backend tests and manual frontend checks were completed successfully.

### Automated Backend Tests

The backend uses **xUnit**. Integration tests exercise the API with `WebApplicationFactory` and EF Core InMemory, including:

- Credit card issuance and lien reservation.
- Rejection of invalid redemption requests.
- Credit card usage and repayment.
- Card closure and lien release.

Run the tests with:

```powershell
cd src/backend
dotnet test PortfolioSecuredCards.sln
```

### Frontend & End-to-End Validation

The Angular frontend was checked manually for customer selection, portfolio display, credit-card operations, usage-history display, and manual delinquency scanning. **The completed test runs passed.**

---

## Documentation

Additional analysis and design documents are located in [`docs/`](docs/):

1. [Business Overview](docs/01-business-overview.md)
2. [Requirements & Decisions](docs/02-requirements.md)
3. [Domain Design](docs/03-domain-design.md)
4. [Solution Architecture](docs/04-solution-architecture.md)

---

## Assumptions and Limitations

- This is a demonstration module, **not** a production banking system.
- EF Core InMemory and Hangfire.InMemory are volatile; application data and background-job state are lost when the process restarts.
- EF Core InMemory does not provide production-grade transactions, isolation, or concurrency control. Saving related changes in one `SaveChanges` is not an ACID guarantee.
- Authentication and authorization are outside scope. The `X-Demo-Customer-Id` header is **not** a security mechanism.
- CORS permits a specific development origin but does not authenticate requests or authorize users.
- Usage and repayment are simulated; there is no payment gateway, interest calculation, or billing cycle.
- Delinquency is based on continuous full utilization beyond a configured interval, not on real statement or payment-due cycles.
- The manual delinquency scan endpoint is intended only for demonstration and must not be exposed without appropriate protections in production.
- Production deployment would require durable storage, proper authentication and authorization, concurrency handling, secure card-data practices, and operational monitoring.
