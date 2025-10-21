import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ROUTES } from 'src/app/constants';

@Component({
  selector: 'app-auth-callback',
  template: '<div>Signing in...</div>',
})
export class AuthCallbackComponent implements OnInit {
  constructor(private route: ActivatedRoute, private auth: AuthService) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      const code = params['code'];
      if (code) {
        const redirectUri = window.location.origin + '/auth-callback';
        this.auth.completePkce(code, redirectUri).subscribe({
          error: (e) => {
            console.error(e);
            window.location.href = ROUTES.DASHBOARD;
          },
          complete: () => {
            window.location.href = ROUTES.DASHBOARD;
          },
        });
        return;
      }

      window.location.href = ROUTES.DASHBOARD;
    });
  }
}
