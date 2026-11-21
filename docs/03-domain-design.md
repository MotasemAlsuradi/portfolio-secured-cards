# 03 — Domain Design

**Project:** Portfolio Secured Cards  
**Module:** Investment-Backed Credit Card  
**Phase:** 03 — Domain Design  
**Status:** Finalized Phase 03 design baseline (v3) — implementation details to be verified in code and tests  
**Related documents:** [01 — Business Overview](01-business-overview.md) · [02 — Requirements & Decisions](02-requirements.md)

> **Scope and precedence:** This design implements the simulated secured-credit module described in the coding assignment. The assignment is authoritative for mandatory behavior; refinements such as simplified repayment, closure and unfreezing come from the accepted decisions in `02-requirements.md`. No real banking integration, billing engine, or collateral liquidation is modeled.

---

## 1. Design Goals

- Model **Portfolio**, **Lien** and **CreditCard** as meaningful domain concepts, not just CRUD records.
- Keep collateral and redemption rules inside the Domain layer.
- Enforce `0 < CreditLimit <= Lien.Amount` without a fixed percentage. The issuance form defaults the requested credit limit to the lien amount but permits the user to lower it; the backend validates and sets the final value (D7). This is an explicit UI design choice, not a rule imposed by the assignment.
- Preserve aggregate-level invariants; coordinate operations involving multiple aggregates in Application command handlers.
- Make status transitions and time-dependent delinquency behavior explicit and testable.
- Avoid unnecessary domain abstractions and keep the model suitable for the two-day assignment.

## 2. Aggregate Decision — ADR-001

**Status: Accepted**  
**Selected approach: Option A — Portfolio owns Liens; CreditCard is a separate Aggregate Root.**

```text
Customer (simple seeded entity)
   1
   │ owns
   *
Portfolio [Aggregate Root]
   1
   │ owns and protects collateral
   *
Lien [Entity within Portfolio aggregate]
   1
   │ secures
   1
CreditCard [Independent Aggregate Root]
   ├── PortfolioId
   └── LienId
```

The diagram shows **business relationships**, not a requirement for direct object navigation across aggregate boundaries. `CreditCard` refers to the other aggregate by identifiers. A portfolio can secure several cards through several liens; in this simulation **each issued card has one dedicated lien, and each lien secures one card** (D2).

### Responsibilities and rationale

| Boundary | Owns | Enforces |
|---|---|---|
| `Portfolio` aggregate | Portfolio state and its `Lien` collection | Available value, total unreleased liens, redemption, lien reservation, freeze/unfreeze state |
| `CreditCard` aggregate | Card utilization, limit, timestamps and lifecycle | Credit-limit bounds at issuance, permitted card state transitions, utilization and repayment bounds, delinquency eligibility |
| Application command handlers | Cross-aggregate use-case coordination | Portfolio eligibility for card usage, issuance/closure ordering, delinquency freeze, unfreeze after final delinquent card closes |
| `Customer` | Basic demonstration identity | No rich customer lifecycle or authentication in scope |

**Why not a single Portfolio aggregate containing all cards?** It would unnecessarily load/couple the entire portfolio whenever a card's frequently changing utilization is updated. **Why not make Lien an independent aggregate?** The critical invariant `Σ(Unreleased Liens) <= TotalValue` is easiest to maintain inside the Portfolio boundary.

**Accepted trade-off:** Card usage reads the portfolio's status separately from updating the card. Another operation might freeze the portfolio between the check and persistence. EF Core In-Memory does not provide production-grade isolation or transactional guarantees; this limitation must be documented rather than misrepresented as solved.

---

## 3. Domain Entities and Value Definitions

### 3.1 Customer — simple entity

| Property | Suggested C# type | Notes |
|---|---|---|
| `Id` | `Guid` | Stable identity |
| `FullName` | `string` | Demonstration display data |

Seed multiple customers. Each `Portfolio` stores `CustomerId`. There is no registration, KYC, authentication or customer-management aggregate. API use cases must still verify that the selected demo customer owns the referenced resources (BR-01, AC-26).

### 3.2 Portfolio — Aggregate Root

| Property | Suggested C# type | Notes |
|---|---|---|
| `Id` | `Guid` | Identity |
| `CustomerId` | `Guid` | Owning customer |
| `Type` | `PortfolioType` | Basic / Plus / Premium |
| `Status` | `PortfolioStatus` | Active / Frozen |
| `TotalValue` | `decimal` | Nonnegative modeled total |
| `Liens` | Read-only collection of `Lien` | Internally managed; no public arbitrary mutation |
| `TotalLienAmount` | Calculated `decimal` | Sum of unreleased lien amounts |
| `AvailableValue` | Calculated `decimal` | `TotalValue - TotalLienAmount` |

**Behaviors** (intended public domain operations; final signatures may vary):

| Behavior | Required domain behavior |
|---|---|
| `ReserveLien(amount, now)` | Only Active; require positive amount `<= AvailableValue`; create and return a new unreleased Lien; do not alter TotalValue |
| `Redeem(amount)` | Only Active; require positive amount `<= AvailableValue`; subtract amount from TotalValue; return a clear business failure otherwise |
| `ReleaseLien(lienId, now)` | Find the owned unreleased lien and release it **only through an eligible card-closure workflow**; reject an already-released lien with `LienAlreadyReleased`; must not release other liens |
| `Freeze()` | Transition Active → Frozen; repeated call is a no-op to support idempotent enforcement |
| `UnfreezeAfterDelinquencyResolution()` | Transition Frozen → Active only after the Application handler has verified no delinquent cards remain |

**Important boundary limitation:** `Portfolio` cannot independently inspect a `CreditCard` aggregate's status. Therefore, releasing a lien only after closing its card, and unfreezing only after the last delinquent card closes, are **cross-aggregate policies**, coordinated by the relevant command handler. The method signatures above do not independently prove those preconditions. Do not expose independent API endpoints that bypass these workflows. Keep release/unfreeze entry points restricted to trusted application workflows, and test the handler-level orchestration.

`AvailableValue` is a **financially non-liened amount**, not a guarantee of permission to redeem. For a Frozen portfolio, `AvailableValue` can remain positive while redemption is forbidden.

### 3.3 Lien — Entity owned by Portfolio

| Property | Suggested C# type | Notes |
|---|---|---|
| `Id` | `Guid` | Identity |
| `PortfolioId` | `Guid` | Owning portfolio |
| `Amount` | `decimal` | Positive; immutable after creation |
| `Status` | `LienStatus` | Unreleased / Released |
| `CreatedAt` | `DateTimeOffset` | Time reserved |
| `ReleasedAt` | `DateTimeOffset?` | Set on release |

The lien reserves investment value but does not subtract it from the portfolio's `TotalValue`. Only unreleased liens count in `TotalLienAmount`. A lien remains unreleased if its card is Blocked or Delinquent. Released liens are retained for historical traceability rather than deleted.

**Behavior:** a guarded internal `Release(now)` transition from Unreleased to Released, invoked through `Portfolio`; attempting to release a lien again fails with `LienAlreadyReleased` and makes no state/audit changes. No arbitrary external update to Amount or Status.

### 3.4 CreditCard — independent Aggregate Root

| Property | Suggested C# type | Notes |
|---|---|---|
| `Id` | `Guid` | Identity |
| `PortfolioId` | `Guid` | Reference to collateral aggregate |
| `LienId` | `Guid` | Dedicated collateral lien |
| `CardNumber` | `string` | Synthetic identifier; masked in customer-facing read results |
| `CreditLimit` | `decimal` | Independent value; `0 < CreditLimit <= Lien.Amount` at issue time |
| `UtilizedAmount` | `decimal` | Initially zero; never negative or above limit |
| `AvailableCredit` | Calculated `decimal` | `CreditLimit - UtilizedAmount` |
| `Status` | `CreditCardStatus` | Active / Blocked / Delinquent / Closed |
| `IssuedAt` | `DateTimeOffset` | Issue time |
| `FullyUtilizedSince` | `DateTimeOffset?` | Time current continuous full-utilization period began |
| `ClosedAt` | `DateTimeOffset?` | Set on closure |

**Behaviors:**

| Behavior | Required domain behavior |
|---|---|
| `Issue(portfolioId, lienId, lienAmount, requestedCreditLimit, cardNumber, now)` | Validate `0 < requestedCreditLimit <= lienAmount`; create Active card, utilization zero. The system **sets the card limit to the validated requested value**, rather than automatically equating it to the lien |
| `Use(amount, now)` | Only Active card; require positive amount not exceeding AvailableCredit; increment utilization; set `FullyUtilizedSince` when the limit is first reached |
| `Repay(amount)` | Require positive amount `<= UtilizedAmount`; reduce utilization; clear `FullyUtilizedSince` if utilization drops below limit; allowed on Delinquent card |
| `MarkDelinquent(now, threshold)` | Transition an eligible Active card to Delinquent only after continuous full utilization reaches the configured threshold; repeated enforcement does nothing |
| `Close(now)` | Require zero utilization; permit Active/Blocked/Delinquent → Closed; reject a second close with `CardAlreadyClosed` (no duplicate release/audit); mark closure time; no further usage |
| `Block()` / `Unblock()` | Optional low-priority manual operations only (D9); never unblock a Delinquent/Closed card |

`CreditCard.Use` enforces **card-local invariants**. The Application handler separately verifies that the linked portfolio is Active before invoking it. The `CreditCard` aggregate does not need to own or navigate to a `Portfolio` object.

`Repay` is a deliberately simplified balance adjustment, not a payment gateway, settlement or billing engine. A Delinquent card may be repaid while the portfolio is Frozen; repayment **does not** automatically return card status to Active.

### 3.5 Card usage history and AuditEntry

**Required by the assignment:** Every successful card-usage operation must be traceable. Persist a usage record containing at least **CardId, amount, UtilizedAmountBefore, UtilizedAmountAfter and timestamp**. Record it in the same intended save boundary as the utilization update, and do not create a usage record for a rejected request. `FullyUtilizedSince` is *not* a substitute for usage history.

**Optional extended audit (D10):** A lightweight append-only audit log may additionally capture lien reservation/release, repayments, redemption, closure, delinquency and portfolio freeze/unfreeze. A single lightweight `AuditEntry` store can serve both needs if it captures the required usage fields; otherwise, use a small dedicated card-usage history record. These records are **not** part of the aggregates' financial invariants and do not imply Event Sourcing. Exact schema and persistence location belong to Phase 04.

---

## 4. Domain Enums

```csharp
public enum PortfolioType
{
    Basic = 1,
    Plus = 2,
    Premium = 3
}

public enum PortfolioStatus
{
    Active = 1,
    Frozen = 2
}

public enum LienStatus
{
    Unreleased = 1, // Still counts as collateral
    Released = 2
}

public enum CreditCardStatus
{
    Active = 1,
    Blocked = 2,
    Delinquent = 3,
    Closed = 4
}
```

**No Portfolio `Closed` status** and no `FreezeReason` enum in this version. Only delinquency can freeze a portfolio. Introducing additional freeze causes later would require reviewing the unfreeze policy, not merely adding enum entries.

---

## 5. Lifecycles and Time Rules

### 5.1 Portfolio

```text
Active ── delinquency enforcement ──> Frozen
Frozen ── final delinquent card is closed ──> Active
```

When Frozen, the portfolio rejects **redemptions and new liens/cards**, and its associated cards reject **new utilization**. **Repayments and eligible card closures remain possible** so the customer can resolve the simulated delinquency.

### 5.2 Lien

```text
Unreleased ── associated card closed ──> Released
```

A lien cannot be released because the card is merely Blocked, fully utilized, repaid, or Delinquent. An already `Released` lien cannot be released again; reject with `LienAlreadyReleased`.

### 5.3 CreditCard

```text
                       ┌──> Blocked ──> Active  [manual, optional]
                       │        │
Issue ──> Active ───────┤        └───────────────┐
                       └──> Delinquent ─────────┤
                               │                │
Active (with zero utilization) ─┼───────────────> Closed
                               └── if zero ─────┘
```

`Closed` is terminal. Eligible closure from Active, Blocked or Delinquent requires `UtilizedAmount == 0`. A second `Close` request is rejected with `CardAlreadyClosed` (AC-25); there is no duplicate lien release or audit event.

### 5.4 FullyUtilizedSince

| Situation | Rule |
|---|---|
| Utilization below limit | `FullyUtilizedSince == null` before enforcement |
| Card first reaches full limit | Set timestamp to `now` |
| Remains at full limit | Preserve timestamp (do not reset at each scan) |
| Repayment drops utilization below full limit | Clear timestamp |
| Reaches full limit again | Start a new timestamp |
| Remains Delinquent after repayment | Card status stays Delinquent; timestamp may clear because current utilization is no longer full |

**Delinquency rule:** Active card + full utilization + elapsed threshold (`now - FullyUtilizedSince >= threshold`) → Delinquent and freeze associated portfolio. A configurable threshold and test-controlled clock are required for deterministic tests. This is deliberately **not** statement-based delinquency. The initial implementation does not expose manual Block/Unblock actions (D9 is optional), so normal users cannot enter the `Blocked` state. If manual blocking is added later, explicitly reconsider whether a fully utilized Blocked card should become Delinquent rather than silently exempting it.

---

## 6. Domain Invariants and Policies

| ID | Rule | Enforcement owner |
|---|---|---|
| INV-01 | `Portfolio.TotalValue >= 0` | Portfolio |
| INV-02 | `Σ(Unreleased Liens) <= Portfolio.TotalValue`; therefore derived `AvailableValue >= 0` | Portfolio |
| INV-03 | Each lien's `Amount > 0`; new lien `<= AvailableValue` | Portfolio / Lien |
| INV-04 | `0 < CreditLimit <= Lien.Amount` at issuance | CreditCard factory with trusted supplied lien amount; issuing handler ensures associated lien |
| INV-05 | `0 <= UtilizedAmount <= CreditLimit` | CreditCard |
| INV-06 | All usage, repayment and redemption request amounts must be positive; repayment `<= UtilizedAmount` | Respective entities |
| INV-07 | Usage only when card Active **and** linked portfolio Active | CreditCard + Application handler |
| INV-08 | ReserveLien and Redeem only while portfolio Active | Portfolio |
| INV-09 | Redemption `<= AvailableValue`; success reduces TotalValue without changing unreleased liens | Portfolio |
| INV-10 | A card closes only if `UtilizedAmount == 0`; Closed card cannot be used | CreditCard |
| INV-11 | Lien release occurs only when its associated card is closed; an already Released lien cannot be released again | Close-card handler + Portfolio-owned transition |
| INV-12 | `FullyUtilizedSince` tracks the present continuous full-utilization period; reset when utilization drops below limit | CreditCard |
| INV-13 | Delinquency enforcement skips cards already Delinquent and does not duplicate changes | CreditCard + job orchestration |
| INV-14 | A portfolio unfreezes only after the last linked Delinquent card closes | Close-card handler + Portfolio transition |
| INV-15 | Frozen portfolio blocks new utilization on **all** its cards, but permits repayments and eligible closure | Application handler + relevant domain operations |
| INV-16 | A Closed card cannot be closed again; repeated requests fail with `CardAlreadyClosed`, without new lien or audit effects | CreditCard + close-card handler |

**Terminology:** INV-07, INV-11, INV-14 and INV-15 are **cross-aggregate business policies**, not invariants a single aggregate can guarantee in isolation. They require coordinated command handling and have the documented limitations of the In-Memory store.

---

## 7. Cross-Aggregate Use-Case Flows

### 7.1 Issue card — UC-002

```text
IssueCreditCardCommandHandler
  1. Load and verify customer's Portfolio.
  2. Validate positive lien and requested limit, including creditLimit <= lienAmount, BEFORE mutating Portfolio. (The UI defaults creditLimit to lienAmount but allows a lower input.)
  3. portfolio.ReserveLien(lienAmount, now)
  4. CreditCard.Issue(portfolioId, lien.Id, lien.Amount, creditLimit, cardNumber, now)
  5. Stage both changes and SaveChanges once.
  6. Return issued card reference / failure reason.
```

If validation fails, do not persist either record. Stage/calculate values carefully so a failure during card construction does not leave a mutated tracked aggregate accidentally saved later. **One SaveChanges is not an ACID transaction guarantee with EF Core In-Memory** (D8).

### 7.2 Use card — UC-003

```text
UseCreditCardCommandHandler
  1. Load card; resolve its Portfolio.
  2. Verify selected customer's ownership and portfolio Status == Active.
  3. card.Use(amount, now)  // card-local validation
  4. Persist card and REQUIRED usage history (amount, before/after utilized, timestamp) in the same intended save boundary.
```

Reading portfolio status and updating card utilization are **not** atomically isolated in this test storage provider.

### 7.3 Delinquency job — UC-005

```text
DelinquencyScan (Hangfire-triggered Application orchestration)
  1. Find candidate Active cards at full utilization.
  2. For each candidate:
       - Re-evaluate eligibility against now and configured threshold.
       - card.MarkDelinquent(now, threshold).
       - portfolio.Freeze().
       - Persist state changes; record audit only for actual transitions.
       - Log failures and continue to independent candidates where possible.
  3. Already-Delinquent cards must not be reprocessed.
```

The business rule must remain testable without executing the scheduler. **Hangfire is infrastructure**; it should invoke the application use case rather than contain financial rules itself. Idempotency covers status changes, duplicate audit effects and repeated job invocations.

### 7.4 Repay — UC-007

```text
RepayCreditCardCommandHandler
  1. Load card and verify customer ownership.
  2. card.Repay(amount) even if card is Delinquent / portfolio is Frozen.
  3. Persist reduced utilization and history/audit.
```

This does **not** change card Delinquent status and does **not** remove the lien.

### 7.5 Close card — UC-008

```text
CloseCreditCardCommandHandler
  1. Load card and its Portfolio; verify customer ownership.
  2. Reject already Closed with CardAlreadyClosed; require zero utilization.
  3. card.Close(now)
  4. portfolio.ReleaseLien(card.LienId, now)  // only as part of this verified closure workflow
  5. Query AnyOtherDelinquentCards(portfolioId, excludingCardId: card.Id).
  6. If portfolio is Frozen and none remain, unfreeze it.
  7. Stage all changes and SaveChanges once; emit audit changes only on actual transitions.
```

The check for remaining delinquent cards must evaluate the **post-closure state**, not count the card just closed as still Delinquent. In particular, exclude `card.Id` from the repository query because the changed card may not yet be persisted. If other delinquent cards remain, the portfolio stays Frozen. Only the closed card's own lien is released. EF In-Memory cannot guarantee production-grade atomicity across these changes.

### 7.6 Redeem — UC-004

```text
RedeemPortfolioCommandHandler
  1. Load and verify customer's Portfolio.
  2. portfolio.Redeem(amount)  // all financial restrictions in Domain
  3. Persist and return new balances / clear rejection reason.
```

The handler **must not** bypass `Portfolio.Redeem` with arithmetic in API or Application code.

---

## 8. Read Model, Errors, and Test Strategy

**CQRS boundary:** Commands mutate aggregates through domain methods. Queries return read DTOs (e.g., portfolio summary and masked card details); queries should not mutate aggregate state. Handler and read-model interfaces are specified during Phase 04, not by adding speculative dependencies to Domain.

**Domain errors:** Rejected operations should produce recognizable codes/reasons such as `InsufficientAvailableValue`, `PortfolioFrozen`, `CreditLimitExceeded`, `CardNotActive`, `OutstandingBalance`, `InvalidAmount`, `CardAlreadyClosed`, and `LienAlreadyReleased`. Domain rules must not depend on HTTP exceptions; the API maps domain/application failures to consistent responses.

**Minimum test cases (Domain unit, Application handler, and integration as appropriate):**

- Multiple liens, exact-available lien, over-available lien, and negative/zero values.
- Exact-available redemption; over-available redemption; any redemption on Frozen portfolio.
- Credit limit less than/equal to lien allowed; greater than lien rejected.
- Card usage exactly at available credit, beyond limit, and when not Active.
- Timestamp set at full utilization, preserved, cleared after repayment and restarted on renewed full utilization.
- No delinquency before threshold; delinquency at threshold; repeated enforcement is idempotent.
- Closing an already Closed card fails without state changes; releasing an already Released lien fails without duplicates.
- Closing with outstanding utilization rejected; successful close releases only its lien.
- Portfolio remains Frozen while another delinquent card exists; unfreezes after last delinquent card closes.

**Test ownership:** A unit test of `CreditCard.Use` covers card-local checks only. Frozen-portfolio sibling-card rejection (AC-17), ownership validation (AC-26), the closed-card/lien-release workflow, and automatic unfreeze are **Application handler tests or integration tests** because they require cross-aggregate data. Use handler unit tests with repository fakes as appropriate; do not claim a pure CreditCard domain unit test proves cross-aggregate restrictions.

Follow the full acceptance criteria in `02-requirements.md` (AC-01 through AC-27). AC-25 permits rejection or a documented no-op; **this design chooses rejection** for clarity. Integration tests should cover representative API workflows and the cross-aggregate issuance/closure paths as time permits.

---

## 9. Implementation Boundaries and Deferred Details

- **No code in this document:** Method names/signatures are design intent, not a committed public API. Prefer `private set`, private collection backing fields, and explicit guarded methods when coding.
- **Currency:** Use `decimal` for monetary amounts; use AED in seed examples. For this AED-only simulation, validate monetary inputs to **at most two decimal places** (no silent rounding of excess precision). No FX conversion or real currency valuation is modeled. This is a design decision, not a mandate in the assignment.
- **Persistence:** EF Core In-Memory is mandatory for module data. It is not production-grade transaction/isolation storage; address those limitations plainly in README.
- **Hangfire storage:** Select a compatible provider in Phase 04. EF Core In-Memory does not automatically provide Hangfire's own storage.
- **Card number:** Synthetic, uniquely generated within the demo dataset, masked in user-facing responses; formatting beyond this remains optional.
- **Optional manual block/unblock:** Not implemented in the initial flow. `Blocked` remains defined as a status required by the assignment; no command can enter it until D9 is implemented, at which point its interaction with delinquency must be explicitly specified.
- **Future freeze reasons:** If Fraud/Regulatory freezes become real requirements, revisit unfreezing and model concurrent freeze causes; do **not** assume today's `Frozen -> Active` policy works for them.
- **Customer ownership:** Every card command (use, repay, close) resolves its Portfolio (or queries the ownership mapping) and verifies `Portfolio.CustomerId` against the selected demonstration customer. `CreditCard` need not duplicate `CustomerId`.
- **Deferred to Phase 04:** Project references/dependency direction, repository interfaces, persistence mappings, background-job adapter, API DTOs, and exact exception/result contracts.

---

## 10. Phase 03 Exit Criteria

- [x] Core entities and relationships identified.
- [x] Portfolio/Lien and CreditCard aggregate boundaries selected (ADR-001).
- [x] Portfolio, Lien, CreditCard and Customer properties and behaviors designed.
- [x] Status transitions and time-dependent delinquency behavior defined.
- [x] Financial invariants separated from cross-aggregate policies.
- [x] Issuance, usage, redemption, delinquency, repayment and closure flows documented.
- [ ] Verify design choices with compiling Domain implementation and focused unit tests during development.

**Next:** Phase 04 — Solution Architecture and CQRS/Infrastructure design, then implementation guided by this baseline.
