import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: any) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          // Attempt to refresh token if unauthorized
          return this.authService.tryRefresh().pipe(
            switchMap((success) => {
              if (success) {
                // Retry the original request after refresh
                return next.handle(req);
              }
              // If refresh fails, propagate error
              return throwError(() => error);
            })
          );
        }
        return throwError(() => error);
      })
    );
  }
}
