# Investment-Backed Credit Card Module

## Phase 01 — Business Overview

### 1. Project Overview

The Investment-Backed Credit Card Module simulates a secured credit product in which customers obtain credit cards backed by their investment portfolios.

Instead of issuing credit solely based on the customer's creditworthiness, the system uses a specified portion of an investment portfolio as collateral.

The collateral is secured through a **lien**, which restricts redemption of the secured amount while allowing the remaining non-liened portfolio value to be redeemed, subject to portfolio restrictions.

The module focuses on collateral management, credit card issuance, credit utilization, portfolio redemption, and automated delinquency enforcement.

> This project is a simplified simulation of real secured credit products used by financial institutions, not a production banking platform.

### 2. Business Objectives

The module aims to:

1. Enable customers to obtain credit cards backed by investment portfolios.
2. Secure credit exposure through portfolio-specific liens.
3. Prevent redemption of investment amounts reserved as collateral.
4. Enforce credit limits and prevent excessive card utilization.
5. Detect delinquent credit cards through scheduled monitoring.
6. Protect investment collateral by freezing portfolios associated with delinquent cards.

### 3. Core Business Concepts

#### 3.1 Customer

A customer is an individual who owns one or more investment portfolios.

Each portfolio belongs to a specific customer.

#### 3.2 Investment Portfolio

An investment portfolio represents a collection of financial assets held by a customer.

For this simulation, portfolios are classified into three types:

- Basic
- Plus
- Premium

Each portfolio maintains the following financial information:

| Property | Description |
|---|---|
| Total Value | The total financial value of the portfolio. |
| Lien Amount | The total amount reserved as collateral through liens that have not been released. |
| Available Value | The non-liened portion of the portfolio. |

For this simplified model:

**Available Value = Total Value − Total Unreleased Lien Amount**

A portfolio may have multiple liens, but their combined amount cannot exceed the portfolio's total value.

#### 3.3 Lien

A lien is a financial restriction placed on a specific amount of an investment portfolio to secure a credit card.

The lien:

- Is associated with one investment portfolio.
- Reserves a specified amount as collateral.
- Prevents redemption of the reserved amount.
- Does not reduce the portfolio's total value.
- Remains effective until it is released.
- Can be released only when the associated credit card is closed.

A portfolio can support multiple liens as long as sufficient non-liened value remains available.

#### 3.4 Credit Card

A credit card provides customers with a credit facility secured by a portfolio lien.

Each card has:

| Property | Description |
|---|---|
| Card Number | A unique identifier representing the issued card. |
| Credit Limit | The maximum credit amount available to the cardholder. |
| Utilized Amount | The amount of credit currently used. |
| Available Credit | The unused portion of the credit limit. |
| Card Status | The card's current operational state. |

**Available Credit = Credit Limit − Utilized Amount**

The card's credit limit cannot exceed its associated lien amount.

A credit card can only be issued after its lien has been successfully established.

### 4. Business Processes

#### 4.1 View Investment Portfolios

Customers can view their investment portfolios, including portfolio type, total value, liened amount, and available value.

#### 4.2 Issue a Credit Card

The customer selects an investment portfolio and specifies a lien amount.

The system validates that sufficient available portfolio value exists before creating a lien and issuing the associated credit card.

The credit limit must not exceed the lien amount.

#### 4.3 Utilize a Credit Card

Customers can simulate credit card usage.

Each successful usage increases the utilized amount.

The system must reject any operation that would exceed the card's credit limit.

When utilization reaches the full credit limit, the card becomes at risk of delinquency.

#### 4.4 Redeem Portfolio Value

Customers may redeem the non-liened portion of their investment portfolios, subject to applicable portfolio restrictions.

The system must reject redemption requests that exceed the available non-liened value.

Redemption restrictions must be enforced by the domain model.

#### 4.5 Monitor Delinquency

A scheduled background process monitors credit card utilization and delinquency conditions.

If a card is fully utilized and remains unpaid beyond a configured period, the system must:

1. Mark the credit card as Delinquent.
2. Freeze the associated investment portfolio.
3. Prevent further prohibited card transactions.

The monitoring process must avoid duplicate enforcement and log processing failures.

### 5. Business Rules Summary

| ID | Business Rule |
|---|---|
| BR-01 | A customer can own multiple investment portfolios. |
| BR-02 | Investment portfolios support Basic, Plus, and Premium types. |
| BR-03 | A portfolio can have multiple liens. |
| BR-04 | A lien cannot exceed the available portfolio value. |
| BR-05 | Total unreleased liens cannot exceed the portfolio's total value. |
| BR-06 | A credit card can only be issued after successful lien creation. |
| BR-07 | A card's credit limit cannot exceed its lien amount. |
| BR-08 | Card utilization cannot exceed the credit limit. |
| BR-09 | Portfolio redemption cannot exceed the available non-liened value. |
| BR-10 | A lien can be released only when its associated card is closed. |
| BR-11 | Full credit utilization places the card at risk of delinquency. |
| BR-12 | A fully utilized card that remains unpaid beyond the configured period becomes delinquent. |
| BR-13 | Delinquency enforcement freezes the associated portfolio and blocks further prohibited card transactions. |

### 6. Scope Boundaries

The module does not implement:

- Payment gateways.
- Interest calculations.
- Full billing and statement cycles.
- Real authentication.
- External banking integrations.

The investment portfolio represents an aggregate financial value. Individual investments, market valuation, and investment trading are not modeled.

### 7. Design Considerations

The implementation will prioritize:

- Enforcement of financial business rules within the Domain layer.
- Separation of business behavior from infrastructure concerns.
- Consistent handling of portfolio liens and credit card operations.
- Testable business logic and clear validation failures.
- Reliable and idempotent delinquency enforcement.

Detailed implementation decisions, including card lifecycle extensions, simplified repayment, portfolio status representation, and lien release behavior, are documented separately in the Requirements and Domain Design phases.
