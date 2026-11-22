import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi
} from '@angular/common/http';

import { App } from './app.component';
import { SecuredCardsModule } from './features/secured-cards/secured-cards.module';
import { CustomerHeaderInterceptor } from './core/interceptors/customer-header.interceptor';

@NgModule({
  declarations: [
    App
  ],
  imports: [
    BrowserModule,
    SecuredCardsModule
  ],
  providers: [
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CustomerHeaderInterceptor,
      multi: true
    }
  ],
  bootstrap: [App]
})
export class AppModule {}