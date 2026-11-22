import { Component, OnInit, ChangeDetectorRef } from '@angular/core';

import { BankingApiService } from '../../../core/services/banking-api.service';
import { CustomerContextService } from '../../../core/services/customer-context.service';

interface Customer {
  id: string;
  fullName: string;
}

@Component({
  selector: 'app-secured-cards',
  standalone: false,
  templateUrl: './secured-cards.component.html',
  styleUrl: './secured-cards.component.css'
})
export class SecuredCardsComponent implements OnInit {

  customers: Customer[] = [];
  selectedCustomerId: string | null = null;

  errorMessage = '';
  scanMessage = '';
  scanError = '';

  scanning = false;

  constructor(
    private readonly api: BankingApiService,
    private readonly customerContext: CustomerContextService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.api.getCustomers().subscribe({
      next: customers => {
        this.customers = customers;
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Customers API Error:', error);

        this.errorMessage = 'Unable to load customers.';
        this.cdr.markForCheck();
      }
    });
  }

  selectCustomer(customerId: string): void {
    this.selectedCustomerId = customerId || null;

    this.customerContext.selectCustomer(this.selectedCustomerId);

    this.scanMessage = '';
    this.scanError = '';
  }

  runDelinquencyScan(): void {
    if (this.scanning || !this.selectedCustomerId) {
      return;
    }

    this.scanning = true;
    this.scanMessage = '';
    this.scanError = '';

    this.api.runDelinquencyScan().subscribe({
      next: () => {
        this.scanning = false;

        this.scanMessage = 'Delinquency scan completed successfully.';

        // Refresh portfolios and credit cards.
        this.customerContext.refresh();

        this.cdr.markForCheck();
      },
      error: error => {
        this.scanning = false;

        const code = error.error?.errorCode;
        const detail = error.error?.detail || 'Delinquency scan failed.';

        this.scanError = code
          ? `${code}: ${detail}`
          : detail;

        this.cdr.markForCheck();
      }
    });
  }
}