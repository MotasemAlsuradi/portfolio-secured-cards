import { ChangeDetectorRef, Component, OnInit, OnDestroy } from '@angular/core';

import { FormBuilder, Validators } from '@angular/forms';

import { Subscription } from 'rxjs';

import { BankingApiService } from '../../../core/services/banking-api.service';
import { CustomerContextService } from '../../../core/services/customer-context.service';

interface Portfolio {
  id: string;
  type: number;
  totalValue: number;
  totalLienAmount: number;
  availableValue: number;
  status: number;
}

type PortfolioAction = 'issue' | 'redeem' | null;

@Component({
  selector: 'app-portfolio-list',
  standalone: false,
  templateUrl: './portfolio-list.component.html',
  styleUrl: './portfolio-list.component.css'
})
export class PortfolioListComponent implements OnInit, OnDestroy {

  portfolios: Portfolio[] = [];

  selectedPortfolio: Portfolio | null = null;
  selectedAction: PortfolioAction = null;

  errorMessage = '';
  successMessage = '';
  submitting = false;

  private readonly subscription = new Subscription();

  readonly issueForm;

  readonly redeemForm;

  constructor(
    private readonly api: BankingApiService,
    private readonly customerContext: CustomerContextService,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef
  ) {
    this.issueForm = this.fb.nonNullable.group({
      lienAmount: [0, [Validators.required, Validators.min(0.01)]],
      creditLimit: [0, [Validators.required, Validators.min(0.01)]]
    });

    this.redeemForm = this.fb.nonNullable.group({
      amount: [0, [Validators.required, Validators.min(0.01)]]
    });
  }

  ngOnInit(): void {

    this.subscription.add(
      this.customerContext.selectedCustomer$.subscribe(customerId => {

        this.portfolios = [];
        this.cancelAction();

        if (customerId) {
          this.loadPortfolios();
        }

        this.cdr.markForCheck();
      })
    );

    // Default credit limit to the entered lien amount.
    this.subscription.add(
      this.issueForm.controls.lienAmount.valueChanges.subscribe(amount => {
        this.issueForm.controls.creditLimit.setValue(amount);
      })
    );

    this.subscription.add(
      this.customerContext.refresh$.subscribe(() => {
        if (this.customerContext.customerId) {
          this.loadPortfolios();
        }
      })
    );
  }

  loadPortfolios(): void {
    this.subscription.add(
      this.api.getPortfolios().subscribe({
        next: portfolios => {
          this.portfolios = portfolios;
          this.cdr.markForCheck();
        },
        error: () => {
          this.errorMessage = 'Unable to load portfolios.';
          this.cdr.markForCheck();
        }
      })
    );
  }

  portfolioType(type: number): string {
    return ['Unknown', 'Basic', 'Plus', 'Premium'][type] ?? 'Unknown';
  }

  openAction(portfolio: Portfolio, action: PortfolioAction): void {

    this.selectedPortfolio = portfolio;
    this.selectedAction = action;

    this.errorMessage = '';
    this.successMessage = '';

    this.issueForm.reset({
      lienAmount: 0,
      creditLimit: 0
    });

    this.redeemForm.reset({
      amount: 0
    });
  }

  cancelAction(): void {
    this.selectedPortfolio = null;
    this.selectedAction = null;
    this.errorMessage = '';
    this.successMessage = '';
  }

  issueCard(): void {

    if (!this.selectedPortfolio || this.issueForm.invalid || this.submitting) {
      return;
    }

    const { lienAmount, creditLimit } = this.issueForm.getRawValue();

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.issueCreditCard(
      this.selectedPortfolio.id,
      lienAmount,
      creditLimit
    ).subscribe({
      next: () => {
        this.onSuccess('Credit card issued successfully.');
      },
      error: error => {
        this.onError(error);
      }
    });
  }

  redeemPortfolio(): void {

    if (!this.selectedPortfolio || this.redeemForm.invalid || this.submitting) {
      return;
    }

    const { amount } = this.redeemForm.getRawValue();

    this.submitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.api.redeemPortfolio(
      this.selectedPortfolio.id,
      amount
    ).subscribe({
      next: () => {
        this.onSuccess('Portfolio redeemed successfully.');
      },
      error: error => {
        this.onError(error);
      }
    });
  }

  private onSuccess(message: string): void {

    this.submitting = false;

    this.selectedPortfolio = null;
    this.selectedAction = null;

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