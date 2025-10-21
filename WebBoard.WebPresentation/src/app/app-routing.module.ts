import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TaskListComponent } from './components/tasks/task-list/task-list.component';
import { TaskCreateComponent } from './components/tasks/task-create/task-create.component';
import { JobListComponent } from './components/jobs/job-list/job-list.component';
import { JobCreateComponent } from './components/jobs/job-create/job-create.component';
import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { SigninComponent } from './components/signin/signin.component';
import { ROUTES, ROUTE_PARAMS } from './constants';
import { AuthGuard } from './services/auth.guard';
import { Injectable } from '@angular/core';
import { Resolve, Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RootRedirectResolver implements Resolve<void> {
  constructor(private auth: AuthService, private router: Router) {}
  async resolve(): Promise<void> {
    const isAuth = await firstValueFrom(this.auth.isAuthenticated$());
    if (isAuth) {
      this.router.navigate([ROUTES.DASHBOARD]);
    } else {
      this.router.navigate([ROUTES.SIGNIN]);
    }
  }
}

const routes: Routes = [
  {
    path: ROUTES.ROOT,
    resolve: { redirect: RootRedirectResolver },
    component: SigninComponent
  },
  {
    path: ROUTES.DASHBOARD,
    component: DashboardComponent,
    canActivate: [AuthGuard],
  },
  { path: ROUTES.TASKS, component: TaskListComponent, canActivate: [AuthGuard] },
  {
    path: ROUTES.TASKS_CREATE,
    component: TaskCreateComponent,
    canActivate: [AuthGuard],
  },
  { path: ROUTES.JOBS, component: JobListComponent, canActivate: [AuthGuard] },
  {
    path: ROUTES.JOBS_CREATE,
    component: JobCreateComponent,
    canActivate: [AuthGuard],
  },
  { path: ROUTES.SIGNIN, component: SigninComponent },
  { path: ROUTES.AUTH_CALLBACK, component: AuthCallbackComponent },
  { path: ROUTES.WILDCARD, redirectTo: ROUTES.DASHBOARD },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
