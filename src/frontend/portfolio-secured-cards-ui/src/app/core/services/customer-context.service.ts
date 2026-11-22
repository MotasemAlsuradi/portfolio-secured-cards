import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class CustomerContextService {

  private readonly selectedCustomerSubject =
    new BehaviorSubject<string | null>(null);

  readonly selectedCustomer$ =
    this.selectedCustomerSubject.asObservable();

  // Notify components when portfolios or cards need refreshing.
  private readonly refreshSubject = new Subject<void>();

  readonly refresh$ = this.refreshSubject.asObservable();

  get customerId(): string | null {
    return this.selectedCustomerSubject.value;
  }

  selectCustomer(customerId: string | null): void {
    this.selectedCustomerSubject.next(customerId);
  }

  refresh(): void {
    this.refreshSubject.next();
  }
}