import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AUTH_CONSTANTS } from '../config/auth.constants';
import {
  map,
  Observable,
  BehaviorSubject,
  catchError,
  of,
  firstValueFrom,
  tap,
} from 'rxjs';
import { ROUTES } from '../constants';
import { AUTH_ENDPOINTS } from '../constants/endpoints';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private pkceStorageKey = 'pkce_code_verifier';
  private isAuthenticated$$ = new BehaviorSubject<boolean>(false);

  public isAuthenticated$(): Observable<boolean> {
    return this.isAuthenticated$$.asObservable();
  }

  constructor(private router: Router, private http: HttpClient) {}

  /**
   * Call the API to check whether the server considers the user authenticated.
   * Returns an observable that emits true/false.
   */
  public checkAuth(): Observable<boolean> {
    return this.http
      .get<{ authenticated: boolean }>(AUTH_ENDPOINTS.CHECK, {
        withCredentials: true,
      })
      .pipe(
        map((r) => !!r.authenticated),
        catchError(() => of(false))
      );
  }

  /**
   * Attempt to call refresh endpoint to get a new access token using the refresh cookie.
   * Returns an observable that emits true when refresh succeeded.
   */
  public tryRefresh(): Observable<boolean> {
    return this.http
      .post<{ access_token: string }>(
        AUTH_ENDPOINTS.REFRESH,
        {},
        { withCredentials: true }
      )
      .pipe(
        map((res) => {
          const success = res?.access_token != null;
          if (success) this.isAuthenticated$$.next(true);
          return success;
        }),
        catchError(() => of(false))
      );
  }

  /**
   * Convenience method for app startup. It checks server auth status and if not
   * authenticated, tries a refresh. It updates the internal isAuthenticated BehaviorSubject.
   */
  public async initializeAuth(): Promise<boolean> {
    try {
      const isAuth = await firstValueFrom(this.checkAuth());
      this.isAuthenticated$$.next(isAuth);
      return isAuth;
    } catch (e) {
      this.isAuthenticated$$.next(false);
      return false;
    }
  }

  async startPkceAuthWithGoogle(redirectUri: string) {
    const clientId = environment.clientIdGoogle;
    const codeVerifier = this.generateCodeVerifier();
    const codeChallenge = await this.sha256(codeVerifier);
    localStorage.setItem(this.pkceStorageKey, codeVerifier);

    const authUrl = new URL(AUTH_CONSTANTS.GOOGLE_AUTH_URL);
    authUrl.searchParams.set('client_id', clientId.toString());
    authUrl.searchParams.set('response_type', 'code');
    authUrl.searchParams.set('scope', 'openid email profile');
    authUrl.searchParams.set('redirect_uri', redirectUri);
    authUrl.searchParams.set('code_challenge', codeChallenge);
    authUrl.searchParams.set('code_challenge_method', 'S256');
    authUrl.searchParams.set('prompt', 'select_account');

    window.location.href = authUrl.toString();
  }

  completePkce(code: string, redirectUri: string): Observable<boolean> {
    const verifier = localStorage.getItem(this.pkceStorageKey);
    if (!verifier) throw new Error('PKCE verifier not found');

    const payload = {
      provider: AUTH_CONSTANTS.GOOGLE_PROVIDER,
      code,
      codeVerifier: verifier,
      redirectUri,
    };

    return this.http
      .post<{ access_token: string }>(AUTH_ENDPOINTS.PKCE_EXCHANGE, payload, {
        withCredentials: true,
      })
      .pipe(
        map((res) => {
          localStorage.removeItem(this.pkceStorageKey);
          const success = res.access_token != null;
          if (success) {
            this.isAuthenticated$$.next(true);
          }
          return success;
        })
      );
  }

  logout() {
    return this.http
      .post(AUTH_ENDPOINTS.LOGOUT, {
        withCredentials: true,
      })
      .pipe(
        tap(() => {
          this.isAuthenticated$$.next(false);
          this.router.navigate([ROUTES.SIGNIN]);
        })
      );
  }

  // Private methods at the bottom
  private base64UrlEncode(buffer: ArrayBuffer) {
    const bytes = new Uint8Array(buffer);
    let str = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      str += String.fromCharCode(bytes[i]);
    }
    return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }

  private async sha256(plain: string) {
    const encoder = new TextEncoder();
    const data = encoder.encode(plain);
    const hash = await crypto.subtle.digest('SHA-256', data);
    return this.base64UrlEncode(hash);
  }

  private generateCodeVerifier() {
    const array = new Uint8Array(32);
    crypto.getRandomValues(array);
    return Array.from(array)
      .map((b) => ('0' + b.toString(16)).slice(-2))
      .join('');
  }
}
