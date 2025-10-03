import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
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
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
  ],
  providers: [],
  bootstrap: [AppComponent],
})
export class AppModule {}
