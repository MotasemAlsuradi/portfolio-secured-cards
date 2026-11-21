# 04 — Solution Architecture

**Project:** Portfolio Secured Cards  
**Module:** Investment-Backed Credit Card  
**Phase:** 04 — Backend Solution Architecture  
**Status:** Accepted backend architecture baseline, revised with ARCH-45–ARCH-51; Angular architecture (04.7) deferred to Phase 07  
**Related documents:** [01 — Business Overview](01-business-overview.md) · [02 — Requirements & Decisions](02-requirements.md) · [03 — Domain Design](03-domain-design.md)

> **Precedence:** The original coding assignment defines mandatory behavior; `02-requirements.md` is the authoritative source for BR/AC identifiers and accepted business refinements; `03-domain-design.md` defines the accepted domain model and aggregate boundaries (ADR-001). This document defines implementation architecture, not new banking product rules. Design examples below are illustrative; exact method signatures and package versions are verified during implementation.

---

## 1. Architecture Objectives and Scope

Build a small, demonstrable **.NET 8** backend that uses **Clean Architecture**, **Strict CQRS** with **MediatR**, **EF Core In-Memory**, and **Hangfire**. Favor separation of concerns, testable financial rules, predictable API behavior, and a straightforward two-day implementation over speculative enterprise infrastructure.

**In scope:** domain/app/infrastructure/API separation; aggregate persistence; read-only query projections; mandatory card-usage history; scheduled delinquency enforcement and manual triggering; consistent error contracts; domain, handler, and minimal API integration tests.

**Out of scope:** real identity/authentication, payment gateway, billing and interest, external bank integrations, event sourcing, message broker, distributed transactions, production-grade persistence, and distributed concurrency controls. Angular remains mandatory in the assignment but its detailed architecture is **deferred, not canceled**.

## 2. Step 04.1 — Solution Structure and Dependencies

### 2.1 Repository layout

```text
portfolio-secured-cards/
├── docs/
│   ├── 01-business-overview.md
│   ├── 02-requirements.md
│   ├── 03-domain-design.md
│   └── 04-solution-architecture.md
├── src/
│   ├── backend/
│   │   ├── PortfolioSecuredCards.sln
│   │   ├── PortfolioSecuredCards.Domain/
│   │   ├── PortfolioSecuredCards.Application/
│   │   ├── PortfolioSecuredCards.Infrastructure/
│   │   └── PortfolioSecuredCards.Api/
│   └── frontend/
│       └── portfolio-secured-cards-ui/   # Angular; Phase 07
├── tests/
│   ├── PortfolioSecuredCards.UnitTests/
│   └── PortfolioSecuredCards.IntegrationTests/
├── .gitignore
└── README.md
```

The solution file resides under `src/backend/`; it can reference test projects using paths outside that folder. These are four production .NET projects plus two test projects, **not** six production layers.

### 2.2 Project responsibilities

| Project | Responsibility | Must not contain |
|---|---|---|
| **Domain** | Customer, Portfolio, Lien, CreditCard; enums; business exceptions and aggregate invariants | EF Core, MediatR, HTTP, Hangfire |
| **Application** | Commands/queries and handlers; use-case orchestration; repository/read contracts, DTOs and time abstractions | Controllers, EF-specific code, Hangfire scheduling |
| **Infrastructure** | `AppDbContext`, EF repositories, read projections, seed data, usage-history persistence, Hangfire adapter | Financial rules duplicated from aggregates |
| **Api** | Controllers, request/response mapping, DI composition, configuration, Problem Details, OpenAPI, endpoint exposure | Portfolio arithmetic or delinquency policy |
| **UnitTests** | Domain tests and Application handler tests with simple in-memory fakes/test doubles | EF Core and real repository implementations |
| **IntegrationTests** | Real EF repository/query behavior, cross-aggregate persistence and in-process API flows via `WebApplicationFactory` | Production infrastructure dependency |

### 2.3 Dependency direction

```text
API ───────────────► Application ───────► Domain
 │                         ▲
 └─────► Infrastructure ───┘
                  └────────────────────► Domain

UnitTests ────────► Domain + Application
IntegrationTests ─► API (+ Infrastructure as needed)
```

These arrows represent **project references**, not runtime data flow. `Application` depends on abstractions, and `Infrastructure` implements them (Dependency Inversion). `Domain` references no application project.

### 2.4 Domain boundary carried forward from ADR-001

- **Portfolio aggregate root** owns and loads its **Lien** collection; its domain methods enforce `Σ(Unreleased Liens) <= TotalValue` and redemption eligibility.
- **CreditCard** is an independent aggregate root identified by its own ID, `PortfolioId`, and `LienId`; it manages limit, utilization, timestamps, delinquency, and lifecycle.
- **Customer** is a simple seeded entity, linked through `Portfolio.CustomerId`.
- **Application handlers** coordinate issuance, card use (portfolio status/ownership), delinquency, closing/releasing a lien, and unfreezing; they do not claim a single aggregate enforces cross-aggregate policies on its own.
- Do **not** create an independent `ILienRepository`; mutations of liens go through `Portfolio`.

---

## 3. Step 04.2 — Strict CQRS with MediatR

**Decision:** Use **Strict CQRS** for application responsibilities and **MediatR** for in-process request dispatch. CQRS is the architectural separation; MediatR is an implementation aid, not a message broker, API gateway, or scalability guarantee.

| Write side — commands | Read side — queries |
|---|---|
| May change application state | Must not change application state |
| Loads aggregates through dedicated repositories | Projects into read DTOs via read services |
| Invokes domain methods and saves via `IUnitOfWork` | Uses no-tracking EF queries where appropriate |
| E.g., issue, use, redeem, repay, close, delinquency scan | E.g., portfolios, card details, cards by customer, usage history |

One request type and one handler per use case; controllers dispatch through `ISender.Send(request, cancellationToken)`. Do not combine reading and writing in a generic CRUD service. **Separate physical databases are not required**: both sides may use the same EF In-Memory store, but use different code paths and return shapes.

**Illustrative structure:**

```text
Application/
├── Features/
│   ├── Portfolios/
│   │   ├── Commands/RedeemPortfolio/
│   │   └── Queries/GetCustomerPortfolios/
│   ├── CreditCards/
│   │   ├── Commands/IssueCreditCard/
│   │   ├── Commands/UseCreditCard/
│   │   ├── Commands/RepayCreditCard/
│   │   ├── Commands/CloseCreditCard/
│   │   ├── Queries/GetCreditCardDetails/
│   │   ├── Queries/GetCustomerCards/
│   │   └── Queries/GetCardUsageHistory/
│   └── Delinquency/Commands/RunDelinquencyScan/
├── Abstractions/Persistence/
├── Abstractions/Time/
└── DTOs/
```

**MediatR package policy:** Target **MediatR `12.5.0`** (previously selected). Confirm the exact NuGet package/version, its license terms and .NET 8 compatibility at package restore; commit the resolved version. Do not silently switch to a later major version with different licensing. No notification bus or advanced pipeline behaviors are required for v1.

**Why this approach:** It keeps HTTP controllers thin, each feature easy to locate, and business rule ownership clear. It improves code maintainability; it does **not**, by itself, increase throughput or provide distributed scale.

---

## 4. Step 04.3 — Persistence, Repositories, and Unit of Work

### 4.1 EF Core In-Memory

Use `Microsoft.EntityFrameworkCore.InMemory` for **module data**, as mandated. Store customers, portfolios, liens, cards, and required usage-history records in one `AppDbContext`. Seed a few named demo customers and multiple portfolios (Basic/Plus/Premium). Application data is volatile on process restart; re-seed deterministically for the demo.

**Provider limitation:** EF In-Memory does not provide real relational transaction/isolation guarantees. A shared `SaveChangesAsync()` coordinates EF tracked changes but is **not** a claim of banking-grade atomicity or concurrency safety. This is an accepted assignment constraint and must be disclosed in README.

### 4.2 Write-side contracts

Place interfaces in `Application/Abstractions/Persistence`; implementations in `Infrastructure/Persistence`.

| Contract | Required responsibilities |
|---|---|
| `IPortfolioRepository` | Load portfolio **with its liens** for domain decisions; add portfolios if necessary |
| `ICreditCardRepository` | Load/add cards, query delinquency candidates, test for other delinquent cards excluding closing card |
| `IUnitOfWork` | `SaveChangesAsync(CancellationToken)` |
| `ICardUsageHistoryWriter` | Stage mandatory successful usage record for persistence |

Do not create a generic repository framework. `AppDbContext : DbContext, IUnitOfWork` implements Unit of Work; no additional UnitOfWork class is needed. A single scoped `AppDbContext` is shared by repositories and the writer within an operation scope. Repositories **stage**, rather than independently save, changes; the handler chooses the save boundary.

**Issue-card flow (UC-002):** verify portfolio/customer and `0 < creditLimit <= lienAmount` **before** mutating the tracked portfolio; call `portfolio.ReserveLien(...)`, construct the `CreditCard` against that lien, stage the card, then `SaveChangesAsync()` **once**. Keep failure paths from accidentally saving mutated tracked objects later.

**Close-card flow (UC-008):** verify customer and `UtilizedAmount == 0`; reject `CardAlreadyClosed`; close card, release **its own** lien through `Portfolio`, query `AnyOtherDelinquentCards(portfolioId, excludingCardId)` (the closing card may not yet be persisted), conditionally unfreeze, and save. A repeated close/release must not produce duplicate history or audit changes.

### 4.3 Query-side read services

Define e.g. `IPortfolioReadService`, `ICreditCardReadService`, and a usage-history read path. EF implementations use `AsNoTracking()` and return DTOs (including computed portfolio availability and **masked** card numbers). Queries do not expose tracked domain aggregates and do not call `SaveChangesAsync()`.

### 4.4 Mandatory usage history

For every successful card-use command, capture **CardId, Amount, UtilizedAmountBefore, UtilizedAmountAfter, Timestamp**. In `UseCreditCardCommandHandler`: read `before = card.UtilizedAmount`, call `card.Use(amount, now)`, read `after = card.UtilizedAmount`, then call `ICardUsageHistoryWriter.AddAsync(...)` to stage an append-only history record. Save the card and history **once through the shared `IUnitOfWork`**. **Do not** append history for rejected use.

**Record ownership:** `CardUsageHistory` is a lightweight **Infrastructure persistence record** mapped by `AppDbContext`, not a new Domain aggregate. The Application layer owns the writer contract and a small history-input DTO carrying the required fields; the Infrastructure adapter maps that DTO to the persistence record. Read-side history uses a dedicated query/read DTO. Extra audit entries for other events are optional (D10); `FullyUtilizedSince` alone does not meet the history requirement.

### 4.5 Money and clock

Use `decimal` for money; for this AED demo accept **at most two decimal places** without silent rounding. Use injectable .NET 8 `TimeProvider` so utilization timestamps and delinquency thresholds can be tested deterministically; in tests, prefer `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` (verify the compatible package version at implementation time). This is a demo monetary policy, not a claim about multicurrency production banking.

---

## 5. Step 04.4 — Hangfire and Delinquency Processing

### 5.1 Trigger and workflow

A recurring Hangfire job triggers `RunDelinquencyScanCommand` through MediatR **hourly**, and a development-only manual endpoint invokes **the same command**. This single job covers both periodic **scanning** (finding eligible cards) and **enforcement** (marking delinquency and freezing portfolios); document this explicitly in the final README. `Cron.Hourly()` fires at the start of each hour; it is not a timer measuring exactly 60 minutes from application startup.

```text
Hangfire recurring trigger ─► DelinquencyScanJob (adapter) ──┐
                                                             ├──► ISender.Send(RunDelinquencyScanCommand)
Development manual endpoint ─────────────────────────────────┘                 │
                                                                               ▼
                                                                    Application handler
                                                                               │
                                                                CreditCard.MarkDelinquent()
                                                                Portfolio.Freeze()
                                                                SaveChanges()
```

**Eligibility:** `Status == Active`, `UtilizedAmount == CreditLimit`, `FullyUtilizedSince != null`, and `now - FullyUtilizedSince >= configured threshold`. Default `DelinquencyThreshold = 24 hours` **for simulation only**, configurable from application settings; a Development override of **1 minute** makes the scan demonstrable using the manual endpoint. It does not represent an actual bank's regulatory delinquency definition.

On a successful transition, the card becomes `Delinquent`, the associated portfolio becomes `Frozen`, and all linked cards are prevented from **new utilization** by the card-use handler's portfolio-status check. A frozen portfolio also rejects redemption and new liens. Repayment and eligible closure remain allowed. The system does not automatically unfreeze a portfolio on repayment; it unfreezes after closing its final delinquent card according to Phase 02/03 decisions.

### 5.2 Job storage and failure behavior

- **Application data:** EF Core In-Memory. **Hangfire job metadata:** a separate Hangfire storage provider, proposed **Hangfire.InMemory**. Confirm package compatibility when implementing. Neither store is durable; restart loses state/history, and the recurring job definition is registered again at startup with a stable ID (e.g. `portfolio-secured-cards-delinquency-scan`).
- **Idempotency:** recheck eligibility before transition; skip already-Delinquent cards; make repeated portfolio `Freeze()` a no-op; do not append duplicate transition audit records. This is logical single-instance idempotency, not an exactly-once guarantee across distributed workers.
- **Error isolation:** log per-card failures and continue scanning other eligible cards. **Do not inject `IServiceScopeFactory` into an Application handler** and do not reuse a `DbContext` containing failed tracked changes. The `RunDelinquencyScanCommandHandler` acts as a scan coordinator and requests candidate IDs through an Application abstraction; it delegates each candidate to `IDelinquencyCardProcessor` (Application interface). The Infrastructure implementation creates a **fresh DI scope** for each card, resolves `ISender` from that scope, and dispatches `ProcessDelinquencyCardCommand(cardId)` to an Application handler that reloads/rechecks the card and linked portfolio, then saves its changes. Failures are logged per candidate by the coordinator/processor; the next candidate gets a fresh scope. This keeps scope creation out of Application and preserves one main scan Command for scheduled and manual triggers (ARCH-23). A single-instance demo still has no distributed exactly-once/concurrency guarantee.
- **Observability:** return or log evaluated, delinquent, frozen, and failed counts. Use `ILogger` for errors; extra persistent job-run history is not required.
- **Dashboard:** make Hangfire Dashboard available **only locally in Development**; do not assume a URL containing `admin` provides authorization.

**Blocked cards:** The initial release does not implement optional manual Block/Unblock (D9). Keep the `Blocked` enum for specification alignment; reconsider its delinquency eligibility if manual blocking is later introduced.

---

## 6. Step 04.5 — REST API, Validation, and Errors

### 6.1 API surface

| Verb | Route | Request kind | Purpose |
|---|---|---|---|
| `GET` | `/api/customers` | Query | List seeded demo customers |
| `GET` | `/api/customers/{customerId}/portfolios` | Query | Show portfolios and balances |
| `POST` | `/api/portfolios/{portfolioId}/redemptions` | Command | Redeem available unencumbered value |
| `POST` | `/api/credit-cards` | Command | Reserve a lien and issue a card |
| `GET` | `/api/customers/{customerId}/credit-cards` | Query | List customer's cards |
| `GET` | `/api/credit-cards/{cardId}` | Query | Masked card details |
| `POST` | `/api/credit-cards/{cardId}/usages` | Command | Record utilization and usage history |
| `POST` | `/api/credit-cards/{cardId}/repayments` | Command | Simulate repayment |
| `POST` | `/api/credit-cards/{cardId}/closure` | Command | Close card and release its lien |
| `GET` | `/api/credit-cards/{cardId}/usage-history` | Query | Inspect required history |
| `POST` | `/api/admin/delinquency/run` | Command | Manual scan, **local Development only** |

Controllers translate HTTP requests to application requests and call `ISender.Send`; they never perform lien arithmetic or implement card-domain decisions. All customer-scoped routes use the **single header contract specified below**. Return `201 Created` for issuance, `200 OK` for queries/commands returning data, and `204 No Content` for successful operations returning none, as appropriate.

**Demo customer context (ARCH-45/46):** All customer-scoped endpoints receive the selected customer ID in the HTTP header `X-Demo-Customer-Id: <guid>` (not duplicated in each request body). The API validates/parses it and exposes it via an `ICurrentCustomer` interface defined in Application and implemented by API using `IHttpContextAccessor` (or equivalent scoped adapter). Handlers **and customer-scoped query handlers** compare `ICurrentCustomer.CustomerId` to `Portfolio.CustomerId`; card operations resolve the linked portfolio. A missing/malformed header on a customer-scoped route returns `400` with `InvalidDemoCustomerId`. A mismatch or inaccessible resource returns the same `404` as a nonexistent resource, avoiding unnecessary disclosure of other demo customers' data. If a route still contains `{customerId}`, it must match the header; alternatively use `/api/me/...` routes in a later API cleanup.

`GET /api/customers` is a public demo selector query and requires **no customer header**. The local Development-only `POST /api/admin/delinquency/run` is a system-level operation, also **without a customer header**. An Angular HTTP interceptor adds the header centrally for all customer-scoped requests. **This is not authentication or secure authorization:** clients can forge the header. Real authentication is outside scope, and the admin endpoint must not be exposed beyond local Development.

**Issue-card contract:** `POST /api/credit-cards` accepts `{ portfolioId, lienAmount, creditLimit }` in its JSON body; customer identity comes **only** from `X-Demo-Customer-Id`. Angular defaults `creditLimit` to `lienAmount` while allowing a lower positive value (D7); the backend sets and validates the final limit.

### 6.2 Errors and validation

Use ASP.NET Core 8 `IExceptionHandler` plus `AddProblemDetails()` to map domain/application failures to consistent Problem Details JSON containing a stable **`errorCode`**, readable `detail`, HTTP `status`, and optional `traceId`. The Domain throws domain-specific errors; it must not reference HTTP types.

| Status | Policy | Example |
|---|---|---|
| `400 Bad Request` | Invalid request syntax/value | Negative amount, malformed input |
| `404 Not Found` | Unknown resource or unavailable to selected demo customer | Unknown/mismatched portfolio or card |
| `409 Conflict` | Business state/rule conflict | See error-code mapping below |
| `500 Internal Server Error` | Unexpected failure | Log server-side; do not leak exception details |

**Stable error-code mapping (ARCH-48):**

| Error code | HTTP | Meaning |
|---|---:|---|
| `InvalidAmount` | 400 | Zero/negative amount or excessive decimal precision |
| `InvalidDemoCustomerId` | 400 | Missing or malformed customer context header on a customer-scoped route |
| `PortfolioNotFound` | 404 | Portfolio missing or not owned by selected demo customer |
| `CardNotFound` | 404 | Card missing or its portfolio not owned by selected demo customer |
| `PortfolioFrozen` | 409 | Prohibited operation on Frozen portfolio |
| `InsufficientAvailableValue` | 409 | Lien/redemption exceeds available unencumbered value |
| `CreditLimitExceedsLien` | 409 | Requested issuance limit exceeds lien amount |
| `CreditLimitExceeded` | 409 | Card usage would exceed remaining credit |
| `CardNotActive` | 409 | Usage attempted while card is not Active |
| `OutstandingBalance` | 409 | Closure attempted with utilized amount above zero |
| `CardAlreadyClosed` | 409 | Closure attempted again on an already Closed card |
| `LienAlreadyReleased` | 409 | Attempt to release an already Released lien |

Keep these identifiers centralized as constants or an enum with stable serialized values, not scattered string literals. Choose a single stable mapping for each error code during implementation and reflect it in OpenAPI and tests. Keep request-shape validation at API/Application boundaries, financial rules in Domain, and cross-aggregate rules in handlers. **Swagger/OpenAPI** is enabled for development. No API versioning is needed for this single-release assignment.

---

## 7. Step 04.6 — Testing Architecture

### 7.1 Testing stack and isolation

Use **xUnit** with a Domain/Application `UnitTests` project and an API `IntegrationTests` project. Integration tests use `Microsoft.AspNetCore.Mvc.Testing`, `WebApplicationFactory`, `HttpClient`, and an **isolated** EF Core In-Memory database/root per test, with controlled seed data. Disable actual Hangfire scheduling/worker execution for integration tests to avoid nondeterministic changes. Tests should not require SQL Server, Docker or cloud services.

### 7.2 Minimum meaningful coverage

| Level | Representative cases | What they prove |
|---|---|---|
| Domain unit | Reserve/redeem exact vs excess value; Frozen restrictions; positive monetary amounts | Portfolio-local invariants |
| Domain unit | `CreditLimit <= lien`; usage bound; full-utilization timestamp set/reset; repayment; close requires zero; repeated close rejected | CreditCard-local invariants |
| Domain unit | Delinquency before/at threshold; repeated transition no-op | Time rule and local idempotency |
| Application handler | Usage rejected when linked Portfolio Frozen, including sibling cards | Cross-aggregate policy |
| Application unit (fakes) | Scan coordinator invokes per-card processor; close handler unfreezes only if fake `AnyOtherDelinquentCards` returns false | Orchestration logic under controlled test doubles |
| Integration (real EF) | Closing final delinquent card unfreezes; closing another while a delinquent card remains keeps Frozen | Correct repository exclusion query **and** persisted aggregate state |
| **Integration #1** | `IssueCreditCard_ShouldCreateCardAndLien` | HTTP → MediatR → repos → EF store |
| **Integration #2** | `RedeemPortfolio_ShouldRejectAmountExceedingAvailableValue` | Error mapping and unchanged persisted value |
| Optional integration #3 | `CloseCreditCard_ShouldReleaseLinkedLien` | Multi-aggregate close flow |

**Test-reference rule (ARCH-47):** `UnitTests` references **Domain + Application only**. Domain tests use entities directly; Application handler tests use straightforward fakes for repository/read/processor interfaces. Fakes validate **decision/orchestration code**, not the correctness of EF queries. Tests that must prove `AnyOtherDelinquentCards` actually excludes the closing card or verify real persistence belong to `IntegrationTests`, which references API and Infrastructure as needed. Do not misclassify handler policies as pure Domain unit tests.

Test the mandatory usage-history persistence alongside the use-card flow: a successful use creates exactly one correct before/after record, and a rejected use none. Use `FakeTimeProvider` or a tiny controllable time provider; avoid real-time waits.

**Scope control:** No load tests, penetration tests, Angular E2E tests, or production database transaction tests in this assignment. Test the application's scan logic, not Hangfire's own Cron implementation.

---

## 8. Configuration and Runtime Composition

`Api/Program.cs` is the **composition root**: configure DI, MediatR handler discovery, EF In-Memory, infrastructure implementations, Problem Details, OpenAPI, Hangfire storage/server/job registration, and seeding. Example conceptual settings:

```json
{
  "Delinquency": {
    "ThresholdMinutes": 1440
  }
}
```

`appsettings.Development.json` (demo override only):

```json
{
  "Delinquency": {
    "ThresholdMinutes": 1
  }
}
```

Use a single `ThresholdMinutes` option with validation (`> 0`) so production-like defaults and the short Development demonstration use the **same** configuration path. A short Development threshold is not an actual banking delinquency rule.

Use one shared scoped `AppDbContext` for repositories/Unit of Work **within an operation**, and isolated scopes for independent background candidates where required. Never share a DbContext across parallel threads. Time comes from an injectable, test-controlled clock. Limit Hangfire Dashboard and manual admin trigger to localhost/development. Add appropriate CORS policy for the local Angular origin later when the frontend port is known; do not use an unrestricted production CORS policy.

Implementation-time package selections (including MediatR, Hangfire, Hangfire.InMemory, EF Core, testing packages) must be pinned to compatible .NET 8 versions and verified at restore/build time. This document does not claim that package installation or code implementation is already complete.

---

## 9. Architecture Decision Register

All entries below record **accepted design decisions**, not already implemented features.

| ID | Decision | Status |
|---|---|---|
| ARCH-01 | Four production projects: Domain, Application, Infrastructure, Api | Accepted |
| ARCH-02 | Two test projects: UnitTests and IntegrationTests | Accepted |
| ARCH-03 | Feature-based Application organization | Accepted |
| ARCH-04 | Inward dependencies; Domain has none | Accepted |
| ARCH-05 | Backend `.sln` inside `src/backend` | Accepted |
| ARCH-06 | Angular frontend under `src/frontend` | Accepted; details deferred |
| ARCH-07 | Strict CQRS with MediatR request dispatch | Accepted |
| ARCH-08 | EF Core In-Memory for application data | Accepted |
| ARCH-09 | Aggregate-specific command repositories | Accepted |
| ARCH-10 | No independent Lien repository | Accepted |
| ARCH-11 | `AppDbContext` implements `IUnitOfWork` | Accepted |
| ARCH-12 | Scoped DbContext per operation scope | Accepted |
| ARCH-13 | Query read services return DTOs using no-tracking reads | Accepted |
| ARCH-14 | Persist mandatory card-usage history with utilization | Accepted |
| ARCH-15 | One save boundary for related changes; no ACID claim | Accepted |
| ARCH-16 | Seed demonstration customers and portfolios | Accepted |
| ARCH-17 | Hourly recurring delinquency scan | Accepted |
| ARCH-18 | Hangfire.InMemory for demo job metadata; compatibility to verify | Accepted |
| ARCH-19 | Hangfire job dispatches Application Command via MediatR | Accepted |
| ARCH-20 | Business logic stays in Domain/Application | Accepted |
| ARCH-21 | Idempotent scan and skip already delinquent cards | Accepted |
| ARCH-22 | Log/isolate per-card failures when possible | Accepted |
| ARCH-23 | Manual trigger dispatches same Application Command | Accepted |
| ARCH-24 | Hangfire Dashboard limited to local development | Accepted |
| ARCH-25 | Delinquency threshold configurable; demo default 24 h | Accepted |
| ARCH-26 | Resource/action-oriented REST endpoints | Accepted |
| ARCH-27 | Thin controllers use MediatR `ISender` | Accepted |
| ARCH-28 | API contracts separate from Domain entities | Accepted |
| ARCH-29 | ASP.NET Core `IExceptionHandler` global error handling | Accepted |
| ARCH-30 | Problem Details with stable `errorCode` | Accepted |
| ARCH-31 | Domain errors do not depend on HTTP | Accepted |
| ARCH-32 | Request/domain/cross-aggregate validation separated | Accepted |
| ARCH-33 | Selected demo customer ownership checked by handlers | Accepted |
| ARCH-34 | Development Swagger/OpenAPI | Accepted |
| ARCH-35 | Manual scan API limited to local development | Accepted |
| ARCH-36 | Read-only API for mandatory usage history | Accepted |
| ARCH-37 | xUnit | Accepted |
| ARCH-38 | Separate UnitTests/IntegrationTests projects | Accepted |
| ARCH-39 | Prioritize domain rule unit tests | Accepted |
| ARCH-40 | Test cross-aggregate policies at handler level | Accepted |
| ARCH-41 | `WebApplicationFactory` for API integration | Accepted |
| ARCH-42 | Isolate EF In-Memory store per test | Accepted |
| ARCH-43 | Disable scheduled Hangfire execution during integration tests | Accepted |
| ARCH-44 | Start with two integration tests; optional third | Accepted |
| ARCH-45 | `X-Demo-Customer-Id` as the single customer-context header for scoped API requests | Accepted |
| ARCH-46 | `ICurrentCustomer` defined in Application, implemented in API; exempt public customer list and local scan endpoint | Accepted |
| ARCH-47 | UnitTests use Domain/Application with fakes; real EF/repository query scenarios belong to IntegrationTests | Accepted |
| ARCH-48 | Centralized stable business error codes and HTTP mappings | Accepted |
| ARCH-49 | Scan coordinator delegates candidate processing through an Application abstraction to an Infrastructure adapter creating fresh per-card scopes | Accepted |
| ARCH-50 | 24-hour default threshold, one-minute Development override for demonstration | Accepted |
| ARCH-51 | Use-card handler captures before/after balances and stages a lightweight history record with the same save | Accepted |

**ADR-001** (Portfolio owns Liens; separate CreditCard aggregate) remains authoritative in `03-domain-design.md`. **D7** (editable limit defaulted to lien amount) and **D10** (mandatory utilization history) remain authoritative in `02-requirements.md`.

---

## 10. Deferred Step 04.7 — Angular Architecture

**Status: Deferred to Phase 07, after backend implementation and Hangfire verification.** The original assignment's Angular requirements **remain mandatory**: use **NgModules (module-based, not standalone-first architecture)** and **Reactive Forms**. Build the UI against implemented API contracts; use a demo-customer selector and header interceptor, portfolio/cards views, issue/use/redeem/repay/close flows, and readable business errors. The issue-card form should default requested `CreditLimit` to `LienAmount` and permit a valid lower amount (D7). Detailed components, routes, services, and form validation will be determined in Phase 07; no Angular implementation is claimed here.

---

## 11. Delivery Sequence and Exit Criteria

**Next:** Phase 05 — Step 01, **create the .NET 8 solution and six projects, configure project references, restore/build successfully**, then implement Domain and Application with focused unit tests, Infrastructure/API, and Hangfire (Phase 06). After the backend is running, return to deferred Angular architecture and implementation (Phase 07). Complete integration checks, README, demo walkthrough, and interview preparation in Phases 08–09.

- [x] Backend projects and allowed dependencies designed.
- [x] Strict CQRS/MediatR separation chosen.
- [x] EF In-Memory, repositories, Unit of Work, history, and seeding designed.
- [x] Hourly Hangfire workflow, manual trigger, idempotency and per-card scope failure handling designed.
- [x] API contracts, uniform demo-customer header context, and global exception/error-code approach designed.
- [x] Unit and minimal integration testing approach designed, with real EF query cases isolated to IntegrationTests.
- [x] Angular architecture explicitly deferred without dropping assignment requirements.
- [ ] Verify package choices, compile the solution, implement features, and demonstrate passing tests during implementation.

**Design baseline completed; no production code, package installation, or test execution is implied by this document.**
