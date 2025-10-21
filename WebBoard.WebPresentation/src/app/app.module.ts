import { NgModule, APP_INITIALIZER } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TaskListComponent } from './components/tasks/task-list/task-list.component';
import { TaskCreateComponent } from './components/tasks/task-create/task-create.component';
import { TaskFormComponent } from './components/tasks/task-form/task-form.component';
import { TaskDetailComponent } from './components/tasks/task-detail/task-detail.component';
import { JobListComponent } from './components/jobs/job-list/job-list.component';
import { JobFormComponent } from './components/jobs/job-form/job-form.component';
import { JobDetailComponent } from './components/jobs/job-detail/job-detail.component';
import { NavbarComponent } from './components/shared/navbar/navbar.component';
import { TaskCardComponent } from './components/tasks/task-card/task-card.component';
import { JobCardComponent } from './components/jobs/job-card/job-card.component';
import { JobCreateComponent } from './components/jobs/job-create/job-create.component';
import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { SigninComponent } from './components/signin/signin.component';
import { CredentialsInterceptor } from './interceptors/credentials.interceptor';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { AuthService } from './services/auth.service';

export function initializeAuthFactory(auth: AuthService) {
  return () => auth.initializeAuth();
}

@NgModule({
  declarations: [
    AppComponent,
    DashboardComponent,
    TaskListComponent,
    TaskCreateComponent,
    TaskFormComponent,
    TaskDetailComponent,
    JobListComponent,
    JobFormComponent,
    JobDetailComponent,
    NavbarComponent,
    TaskCardComponent,
    JobCardComponent,
    JobCreateComponent,
    AuthCallbackComponent,
    SigninComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: CredentialsInterceptor,
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuthFactory,
      deps: [AuthService],
      multi: true,
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
