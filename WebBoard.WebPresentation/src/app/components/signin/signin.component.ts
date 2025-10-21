import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-signin',
  template: `
    <div class="signin-container">
      <h2>Sign In</h2>
      <button (click)="signIn()">Sign in with Google</button>
    </div>
  `,
  styles: [
    `
      .signin-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        margin-top: 40px;
      }
      button {
        margin-top: 20px;
        padding: 10px 20px;
        font-size: 16px;
        cursor: pointer;
      }
    `,
  ],
})
export class SigninComponent {
  constructor(private auth: AuthService) {}

  signIn() {
    const redirectUri = window.location.origin + '/auth-callback';
    this.auth.startPkceAuthWithGoogle(redirectUri);
  }
}
