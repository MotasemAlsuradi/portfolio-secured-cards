import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { SecuredCardsComponent } from './secured-cards/secured-cards.component';
import { PortfolioListComponent } from './portfolio-list/portfolio-list.component';
import { CreditCardListComponent } from './credit-card-list/credit-card-list.component';

@NgModule({
  declarations: [
    SecuredCardsComponent,
    PortfolioListComponent,
    CreditCardListComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  exports: [
    SecuredCardsComponent
  ]
})

export class SecuredCardsModule {}