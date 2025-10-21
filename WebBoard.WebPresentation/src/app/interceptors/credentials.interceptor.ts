import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class CredentialsInterceptor implements HttpInterceptor {
  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // If the request already explicitly set withCredentials, respect it.
    if (req.withCredentials === true) {
      return next.handle(req);
    }

    // Clone the request and set withCredentials to true so cookies are sent/accepted
    const cloned = req.clone({ withCredentials: true });
    return next.handle(cloned);
  }
}
