import { Component } from '@angular/core';
import { ROUTES } from '../../../constants';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent {
  routes = ROUTES;

  constructor(public auth: AuthService) {}

  signInWithGoogle() {
    // Start client-side PKCE flow with Google
    const redirectUri = window.location.origin + '/auth-callback';
    this.auth.startPkceAuthWithGoogle(redirectUri);
  }

  logout() {
    this.auth.logout().subscribe();
  }
}
