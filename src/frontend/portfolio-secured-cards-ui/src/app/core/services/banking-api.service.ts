import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BankingApiService {

  private readonly apiUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  // Customers
  getCustomers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/customers`);
  }

  // Portfolios
  getPortfolios(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/portfolios`);
  }

  redeemPortfolio(portfolioId: string, amount: number): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/portfolios/${portfolioId}/redeem`,
      { amount }
    );
  }

  // Credit Cards
  getCreditCards(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/credit-cards`);
  }

  getCreditCard(cardId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/credit-cards/${cardId}`);
  }

  issueCreditCard(
    portfolioId: string,
    lienAmount: number,
    creditLimit: number
  ): Observable<any> {
    return this.http.post(`${this.apiUrl}/credit-cards`, {
      portfolioId,
      lienAmount,
      creditLimit
    });
  }

  useCreditCard(cardId: string, amount: number): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/credit-cards/${cardId}/use`,
      { amount }
    );
  }

  repayCreditCard(cardId: string, amount: number): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/credit-cards/${cardId}/repay`,
      { amount }
    );
  }

  closeCreditCard(cardId: string): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/credit-cards/${cardId}/close`,
      {}
    );
  }

  getCardUsageHistory(cardId: string): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/credit-cards/${cardId}/usage-history`
    );
  }

  // Delinquency
  runDelinquencyScan(): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/delinquency/scan`,
      {}
    );
  }
}