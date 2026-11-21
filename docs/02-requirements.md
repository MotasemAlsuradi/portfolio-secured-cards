# 02 — Requirements & Decisions

**Project:** Portfolio Secured Cards  
**Module:** Investment-Backed Credit Card  
**Phase:** 02 — Requirements Analysis  
**Status:** Ready for Domain Design (subject to implementation-level choices in Phase 03/04)  
**Scope:** One simulated secured-credit module, not a production banking system.

> **Source precedence:** **[Spec]** means an explicit requirement from the supplied *Net Coding Test.pdf* and is mandatory. **[Decision]** means a deliberate implementation choice for this simulation. **[Optional]** means a good-to-have or extension.
>
> **Confidentiality:** Do not copy the confidential assignment PDF into the public repository. This document describes the proposed implementation without reproducing the assignment.

---

## 1. Business Context and Scope

The module simulates a **real secured credit product**: a customer receives a credit card backed by a **portfolio-specific lien**. A lien reserves investment value as collateral; it is not a withdrawal. Customers can use the issued credit card within its limit and redeem only non-liened, otherwise eligible portfolio value. A Hangfire background process detects the simulated delinquency condition and applies restrictions.

### In scope

- View customers' Basic, Plus, and Premium portfolios and their balances.
- Issue a credit card **only after** successfully reserving its collateral as a lien.
- Simulate credit-card utilization, validate limits, and track changes.
- Redeem available non-liened portfolio value, enforcing restrictions **in the Domain layer**.
- Run periodic delinquency scans, mark qualifying cards Delinquent, freeze linked portfolios, and block card usage.
- Implement minimal simulated repayment and card closure to exercise the delinquency and lien-release lifecycle. **[Decision]**
- Provide an Angular UI to exercise the main flows. **[Spec]**

### Out of scope

Payment gateways, interest calculation, real authentication, external integrations, billing/statements, investment trading/valuation, liquidation and margin calls. No product-specific banking integration is implied.

**Customer selection [Decision]:** Seed 2–3 demonstration customers and their portfolios. Select a demo customer from the UI; pass `CustomerId` in relevant requests. This is **not authentication or authorization**. The backend still validates portfolio/card ownership relative to the selected demo customer.

---

## 2. Domain Vocabulary

| Term | Definition | Source |
|---|---|---|
| Customer | Owns one or more investment portfolios. | Spec |
| Portfolio | An investment container categorized **Basic**, **Plus**, or **Premium**. | Spec |
| Total Value | Total modeled value of a portfolio; individual holdings and market fluctuations are out of scope. | Spec / Decision |
| Lien | Portfolio-specific amount reserved as collateral. | Spec |
| Unreleased Lien | A lien not yet released on card closure; it remains reserved even when its card is **Blocked** or **Delinquent**. | Decision |
| Liened Amount | Sum of all unreleased liens on a portfolio. | Decision |
| Available Value | `Total Value − Sum(Unreleased Lien Amounts)`; a computed **financial** value, not an authorization to redeem from a frozen portfolio. | D1 |
| Credit Limit | Maximum permitted card utilization; must be positive and must not exceed the associated lien amount. | Spec + D7 |
| Utilized Amount | Card credit currently used. | Spec |
| Available Credit | `Credit Limit − Utilized Amount`. | Decision |
| At Risk | Derived condition: `Utilized Amount == Credit Limit`. Not a separate persisted card status. | Spec / Decision |
| Portfolio Status | `Active` or `Frozen` in this version; no portfolio `Closed` state. | D6 |
| Delinquency Threshold | Configured elapsed time during which the card remains at full utilization without repayment. | Spec + D5 |

> **Two separate restrictions:** A lien reserves only its secured amount. A frozen portfolio blocks additional operations even though its computed Available Value may still be positive.

---

## 3. Business Rules

| ID | Business Rule | Source |
|---|---|---|
| BR-01 | One customer may own multiple portfolios; types are Basic, Plus, Premium. | Spec |
| BR-02 | `Available Value = Total Value − Σ(Unreleased Liens)`; derived, not independently mutable. | D1 |
| BR-03 | Each new lien must have `Amount > 0` and `Amount ≤ Available Value`. | Spec + validation decision |
| BR-04 | A portfolio may hold several liens, but `Σ(Unreleased Liens) ≤ Total Value` at all times. | Spec |
| BR-05 | Card issuance is rejected unless the associated lien can be created successfully; no partially issued card/lien should remain after a failed request. | Spec + D8 |
| BR-06 | Each issued card has exactly one associated lien, and a lien secures exactly one card in this simulation. | D2 |
| BR-07 | **Mandatory invariant:** `0 < Credit Limit ≤ Lien Amount`. The system validates the requested credit limit; no fixed percentage is applied. | Spec + D7 |
| BR-08 | Usage amount must be positive, and `Utilized Amount + Usage Amount ≤ Credit Limit`. | Spec + validation decision |
| BR-09 | New usage is allowed only for an `Active` card whose portfolio is `Active`. | Spec + D6 |
| BR-10 | Redemption amount must be positive and not exceed Available Value; a frozen portfolio cannot redeem. Enforce these rules in the Domain layer and return a clear reason. | Spec + D6 |
| BR-11 | Successful redemption reduces portfolio Total Value by the redeemed amount, without modifying unreleased liens. | Decision |
| BR-12 | A lien may be released **only when its card is closed**; repayment by itself never releases a lien. | Spec |
| BR-13 | A card may close only when `Utilized Amount == 0`; eligible closing is allowed from `Active`, `Blocked`, or `Delinquent`. Closure releases its lien. | D3 |
| BR-14 | Upon first reaching full utilization, record `FullyUtilizedSince`. A repayment that reduces utilization below the limit clears it; if full utilization is reached again, record a new timestamp. | D4 + D5 |
| BR-15 | An `Active` card that stays fully utilized without repayment until the configured threshold becomes `Delinquent`; its linked portfolio becomes `Frozen`. | Spec + D5 |
| BR-16 | A frozen portfolio rejects redemption, creation of new liens/cards, and **new card usage on all associated cards**. It still permits repayment and eligible card closure. | D6 |
| BR-17 | Repayment does not automatically change a `Delinquent` card's status or unfreeze its portfolio. Upon closing the **last associated Delinquent card**, the portfolio returns to `Active`, since delinquency is the sole freeze cause modeled here. | D6 |
| BR-18 | The delinquency job is idempotent: skips already-delinquent cards, avoids duplicate state/audit changes, isolates per-card failures where possible, and logs failures. | Spec + D10 |
| BR-19 | Utilization changes must be traceable; an append-only audit log for lien/card/portfolio changes is included as a targeted good-to-have. | Spec + D10 |
| BR-20 | Monetary amounts use `decimal` with the modeled currency precision and positive-value validation. | Decision |

### Credit-limit rule

The mandatory rule is **`0 < Credit Limit ≤ Lien Amount`**. In this simulation, the applicant can specify the requested credit limit alongside the portfolio and lien amount; the system validates the limit without any fixed percentage. Adding that input is a **[Decision]**, since the assignment explicitly lists only portfolio and lien amount as user-selected fields.

**Example (AED):** A lien of `10,000.00` may secure a credit limit of `8,000.00` or `10,000.00`, but not `12,000.00`.

---

## 4. Lifecycle Models

### 4.1 Portfolio

```text
Active ──(Delinquency enforcement)──> Frozen
Frozen ──(Last delinquent card closes)──> Active
```

`PortfolioStatus`: `Active = 1`, `Frozen = 2`. A separate `FreezeReason` and portfolio closure process are not modeled. This can be extended when requirements actually introduce other freeze causes.

### 4.2 Credit Card

```text
Active ──(threshold reached)──> Delinquent
Active ──(optional manual action)──> Blocked
Blocked ──(optional manual action)──> Active
Active / Blocked / Delinquent ──(Utilized == 0; close)──> Closed
```

`Active`, `Blocked`, `Delinquent` are listed in the task. `Closed` is an added terminal state used to enforce lien release on closure. A card cannot be used once Closed. Manual block/unblock UI is optional; regardless, Blocked cards reject usage.

### 4.3 Lien

```text
Unreleased ──(Associated card closes)──> Released
```

A lien stays unreleased while a card is Delinquent. No separate early lien-release workflow is supported.

---

## 5. Use Cases and Acceptance Criteria

### UC-001 — View Portfolios *(Query)*

- **AC-01:** For a selected demo customer, return only that customer's portfolios with type, Total, Liened, Available, and Status.
- **AC-02:** On each portfolio, `Liened + Available = Total`, including when Frozen.
- **AC-03:** Multiple portfolios per customer and multiple demo customers are supported without real authentication.

### UC-002 — Issue Secured Credit Card *(Command)*

- **AC-04:** On an Active portfolio, given `0 < Lien Amount ≤ Available` and `0 < Credit Limit ≤ Lien Amount`, create a lien and an Active card together; initialize utilization to zero, generate a card number, and apply the validated requested limit.
- **AC-05:** Reject zero/negative/excessive lien amounts, invalid credit limits (`≤ 0` or `> lien`), or a Frozen portfolio; leave balances and issuance records unchanged.
- **AC-06:** A failed issuance must not leave a usable orphaned card or orphaned unreleased lien; document test-storage limitations (D8).
- **AC-07:** Creating several liens is allowed until Available Value is exhausted; every card has its own lien.

### UC-003 — Simulate Card Usage *(Command)*

- **AC-08:** Positive usage within Available Credit increases Utilized Amount; exact use of the remaining credit is allowed.
- **AC-09:** Negative/zero usage, over-limit usage, non-Active cards, or cards on Frozen portfolios are rejected without changing utilization.
- **AC-10:** When the utilization reaches the full credit limit, record `FullyUtilizedSince`; usage history/audit records the change.

### UC-004 — Redeem Portfolio Value *(Command)*

- **AC-11:** Positive redemption no greater than the non-liened Available Value reduces Total Value and maintains portfolio lien invariants. An exact-available redemption is permitted.
- **AC-12:** Zero/negative/excessive redemption or Frozen status is rejected **by the Domain**, with a clear reason and no balance change.

### UC-005 — Detect Delinquency *(Hangfire Background Job)*

- **AC-13:** Run periodically (default every 60 minutes), with an optional developer/demo manual trigger; configurable threshold and injectable clock for deterministic tests.
- **AC-14:** Before threshold: no change. At/after threshold: an eligible fully-utilized, unpaid Active card becomes Delinquent; its portfolio becomes Frozen and future usage is blocked.
- **AC-15:** Re-running the scan does not duplicate transitions or audit entries; already-Delinquent cards are skipped.
- **AC-16:** Errors are logged and one card's failure should not prevent independent cards from being checked, within the selected storage implementation's limitations.
- **AC-17:** If one card becomes Delinquent, card usage on its otherwise Active sibling card (same portfolio) is rejected while Frozen.

### UC-006 — View Card Details *(Query)*

- **AC-18:** Return card reference, masked synthetic number, limit, utilized amount, available credit, status, full-utilization/at-risk indicator, linked lien, and portfolio reference.

### UC-007 — Simulate Repayment *(Command — D4)*

- **AC-19:** Positive amount up to Utilized Amount reduces utilization. Any repayment bringing the card below full utilization resets `FullyUtilizedSince`.
- **AC-20:** Repayment is allowed for Delinquent cards even if their portfolios are Frozen; it does not automatically restore card status or portfolio status.
- **AC-21:** Repaying after a full-utilization period and later reaching full utilization starts a new delinquency clock.

### UC-008 — Close Card and Release Lien *(Command — D3)*

- **AC-22:** Reject closure while `Utilized Amount > 0`.
- **AC-23:** Closing an eligible Active, Blocked, or Delinquent card with zero utilization sets its status to Closed and releases only its own lien.
- **AC-24:** Closing the last Delinquent card of a Frozen portfolio returns the portfolio to Active; do not unfreeze while another associated Delinquent card remains.
- **AC-25:** Closing a card that is already Closed is rejected or treated as a documented no-op; it must never release another lien or duplicate audit entries.

### Additional shared validation

- **AC-26:** Requested portfolio/card must belong to the selected demo customer; cross-customer operations are rejected.
- **AC-27:** Business failures return stable, readable API errors (e.g., `InsufficientAvailableValue`, `PortfolioFrozen`, `CreditLimitExceeded`).

---

## 6. Technical and Architectural Requirements

| ID | Requirement | Classification |
|---|---|---|
| TR-01 | Backend built with **.NET 8**. | Spec — Mandatory |
| TR-02 | **Clean Architecture**, with Domain business rules isolated from frameworks. | Spec — Mandatory |
| TR-03 | **Strict CQRS**: distinguish write commands from read queries in the Application layer. | Spec — Mandatory |
| TR-04 | **EF Core In-Memory** for business-data persistence in this test. | Spec — Mandatory |
| TR-05 | **Hangfire** periodic utilization scanning and delinquency enforcement. | Spec — Mandatory |
| TR-06 | Hangfire job **idempotency**, skipping already-Delinquent cards and **failure logging**. | Spec — Mandatory |
| TR-07 | **Angular module-based** frontend (NgModules; avoid standalone-only app). | Spec — Mandatory |
| TR-08 | Angular **Reactive Forms** for user input. | Spec — Mandatory |
| TR-09 | Domain-level enforcement of redemption restrictions. | Spec — Mandatory |
| TR-10 | Audit trail for lien and card usage. | Spec — Good to Have |
| TR-11 | Global API exception handling. | Spec — Good to Have |
| TR-12 | Unit tests for financial invariants, use cases, and job idempotency. | Decision — Delivery Quality |
| TR-13 | Backend API, Angular UI, and concise README with setup/test steps; deliver via GitHub repository. | Assignment email / Delivery |

**Hangfire storage [Pending implementation choice]:** The requirement specifies EF Core In-Memory **for the module data**, not Hangfire's job storage. Select and verify a compatible Hangfire storage provider during Phase 04. If volatile, clearly document restart behavior. Do not claim EF Core In-Memory provides Hangfire storage.

---

## 7. Decisions Log

| ID | Decision | Status |
|---|---|---|
| D1 | Available Value is computed: `Total Value − Σ(Unreleased Liens)`. | Accepted |
| D2 | One lien secures one card (1:1). The exact **aggregate ownership** and repository boundaries are deferred to Phase 03. | Accepted (relationship); design pending |
| D3 | Add card `Closed`; allow close only when utilized is zero, including Delinquent cards; close releases lien. | Accepted |
| D4 | Use a minimal repayment command instead of a payment gateway. | Accepted |
| D5 | Track `FullyUtilizedSince`, reset after repayment below limit, and use configurable threshold plus testable time source. | Accepted |
| D6 | Represent Portfolio with `PortfolioStatus { Active, Frozen }`; delinquency freezes, while repayments and eligible closes remain allowed; unfreeze after the last Delinquent card closes. | Accepted |
| D7 | Enforce `0 < Credit Limit ≤ Lien Amount` with no fixed percentage. The user may specify the requested limit as an additional issuance input. | Accepted |
| D8 | Coordinate lien creation and card issuance as one application operation/save boundary; handle failures; document EF In-Memory transaction/concurrency limitations. | Accepted |
| D9 | Optional manual Block/Unblock commands; never unblock a Delinquent/Closed card. | Proposed / Low priority |
| D10 | Lightweight append-only audit entries to record lien, card usage, redemption, closure, freeze/unfreeze; no Event Sourcing. | Accepted if feasible within delivery |
| D11 | Generate synthetic unique card numbers; mask all customer-facing responses. A 16-digit Luhn-valid format is optional. | Proposed |
| D12 | Seed several customers for demonstration; no authentication. | Accepted |


---

## 8. Delivery Priorities — 2-Day Assignment

| Priority | Deliverables |
|---|---|
| **P0 — Mandatory** | UC-001 to UC-006; mandatory tech stack; strict CQRS; correct domain rules; Angular module-based screens with Reactive Forms; Hangfire scan, idempotency and failure logging. |
| **P1 — Lifecycle & quality** | UC-007 simulated repayment; UC-008 close/release; core domain/unit tests; audit trail where practical; global exception handling; complete README. |
| **P2 — Extras** | Manual Block/Unblock, additional integration/edge testing, sophisticated concurrency implementation, advanced card-number formatting. |

**Implementation order:** Domain rules and tests → application commands/queries → persistence/API → Hangfire → Angular → end-to-end verification and README. Documentation evolves alongside the code.

### Short Definition of Done

A use case is complete when its behavior and rejection paths are implemented, the corresponding Domain rules are tested, API results/errors are understandable, and the relevant UI flow works (when user-facing). The full submission is complete only once all **mandatory** requirements are demonstrably satisfied and the repository includes clear local run/test instructions.

---

## 9. Known Limitations and Assumptions

- The storage is EF Core In-Memory, which does **not** provide production transactional durability, isolation, or general concurrency guarantees. Saving related entities in one `SaveChanges` is an application-level consistency strategy, not a claim of ACID atomicity.
- Hangfire job storage is a separate provider choice; any volatile storage loses job history on restart.
- Delinquency deliberately uses **continuous full utilization without repayment** rather than actual statements, minimum-payment schedules, and overdue amounts.
- The only portfolio freeze cause modeled is **delinquency**. There is no regulatory/fraud freeze management, so automatically unfreezing after the last delinquent card closes is safe **within this simulation only**.
- `PortfolioStatus` does not include Closed; portfolio closure and asset valuation are out of scope.
- No real card-network integration, real PAN issuance, payment settlement, customer authentication, or authorization is implemented.
