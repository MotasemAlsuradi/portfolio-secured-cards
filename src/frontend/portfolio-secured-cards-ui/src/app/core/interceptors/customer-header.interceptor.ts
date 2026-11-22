
import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CustomerContextService } from '../services/customer-context.service';

@Injectable()
export class CustomerHeaderInterceptor implements HttpInterceptor {

  constructor(private readonly customerContext: CustomerContextService) {}

  intercept(
    req: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {

    const customerId = this.customerContext.customerId;

    if (!customerId || !req.url.startsWith('/api/')) {
      return next.handle(req);
    }

    const request = req.clone({
      setHeaders: {
        'X-Demo-Customer-Id': customerId
      }
    });

    return next.handle(request);
  }
}