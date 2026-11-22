import {
  ChangeDetectorRef,
  Component,
  OnInit,
  OnDestroy
} from '@angular/core';

import { FormBuilder, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';

import { BankingApiService } from '../../../core/services/banking-api.service';
import { CustomerContextService } from '../../../core/services/customer-context.service';

interface CreditCard {
  id: string;
  maskedCardNumber: string;
  creditLimit: number;
  utilizedAmount: number;
  availableCredit: number;
  status: number;
}

interface CardUsageHistory {
  id: string;
  cardId: string;
  amount: number;
  utilizedBefore: number;
  utilizedAfter: number;
  createdAt: string;
}

type CardAction = 'use' | 'repay' | null;

@Component({
  selector: 'app-credit-card-list',
  standalone: false,
  templateUrl: './credit-card-list.component.html',
  styleUrl: './credit-card-list.component.css'
})
export class CreditCardListComponent implements OnInit, OnDestroy {

  cards: CreditCard[] = [];

  selectedCard: CreditCard | null = null;
  selectedAction: CardAction = null;

  historyCardId: string | null = null;
  usageHistory: CardUsageHistory[] = [];

  errorMessage = '';
  successMessage = '';

  submitting = false;
  loadingHistory = false;

  private readonly subscription = new Subscription();

  readonly actionForm;

  constructor(
    private readonly api: BankingApiService,
    private readonly customerContext: CustomerContextService,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.actionForm = this.fb.nonNullable.group({
      amount: [0, [Validators.required, Validators.min(0.01)]]
    });
  }

  ngOnInit(): void {
    this.subscription.add(
      this.customerContext.selectedCustomer$.subscribe(customerId => {
        this.cards = [];
        this.selectedCard = null;
        this.selectedAction = null;
        this.historyCardId = null;
        this.usageHistory = [];

        this.errorMessage = '';
        this.successMessage = '';

        if (customerId) {
          this.loadCards();
        }

        this.cdr.markForCheck();
      })
    );

    this.subscription.add(
      this.customerContext.refresh$.subscribe(() => {
        if (this.customerContext.customerId) {
          this.loadCards();
        }
      })
    );
  }

  loadCards(): void {
    this.subscription.add(
      this.api.getCreditCards().subscribe({
        next: cards => {
          this.cards = cards;
          this.cdr.markForCheck();
        },
        error: () => {
          this.errorMessage = 'Unable to load credit cards.';
          this.cdr.markForCheck();
        }
      })
    );
  }

  cardStatus(status: number): string {
    return ['Unknown', 'Active', 'Blocked', 'Delinquent', 'Closed'][status]
      ?? 'Unknown';
  }

  openAction(card: CreditCard, action: CardAction): void {
    this.selectedCard = card;
    this.selectedAction = action;

    this.errorMessage = '';
    this.successMessage = '';

    this.actionForm.reset({ amount: 0 });
  }

  cancelAction(): void {
    this.selectedCard = null;
    this.selectedAction = null;
    this.errorMessage = '';
  }

  submitAction(): void {
    if (
      !this.selectedCard ||
      !this.selectedAction ||
      this.actionForm.invalid ||
      this.submitting
    ) {
      return;
    }

    const cardId = this.selectedCard.id;
    const amount = this.actionForm.getRawValue().amount;
    const action = this.selectedAction;

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const request = action === 'use'
      ? this.api.useCreditCard(cardId, amount)
      : this.api.repayCreditCard(cardId, amount);

    request.subscribe({
      next: () => {
        this.onSuccess(
          action === 'use'
            ? 'Credit card utilized successfully.'
            : 'Repayment completed successfully.'
        );
      },
      error: error => this.onError(error)
    });
  }

  closeCard(card: CreditCard): void {
    if (this.submitting) {
      return;
    }

    if (!confirm('Are you sure you want to close this credit card?')) {
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.closeCreditCard(card.id).subscribe({
      next: () => {
        this.onSuccess('Credit card closed and lien released successfully.');
      },
      error: error => this.onError(error)
    });
  }

  toggleHistory(cardId: string): void {
    if (this.historyCardId === cardId) {
      this.historyCardId = null;
      this.usageHistory = [];
      return;
    }

    this.historyCardId = cardId;
    this.usageHistory = [];
    this.loadingHistory = true;
    this.errorMessage = '';

    this.api.getCardUsageHistory(cardId).subscribe({
      next: history => {
        if (this.historyCardId === cardId) {
          this.usageHistory = history;
          this.loadingHistory = false;
          this.cdr.markForCheck();
        }
      },
      error: error => {
        if (this.historyCardId === cardId) {
          this.loadingHistory = false;
          this.onError(error);
        }
      }
    });
  }

  private onSuccess(message: string): void {
    this.submitting = false;

    this.selectedCard = null;
    this.selectedAction = null;

    this.historyCardId = null;
    this.usageHistory = [];

    this.successMessage = message;

    this.customerContext.refresh();
    this.cdr.markForCheck();
  }

  private onError(error: any): void {
    this.submitting = false;

    const code = error.error?.errorCode;
    const detail = error.error?.detail || 'Operation failed.';

    this.errorMessage = code ? `${code}: ${detail}` : detail;

    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}