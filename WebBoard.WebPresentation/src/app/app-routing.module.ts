import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { TaskListComponent } from './components/tasks/task-list/task-list.component';
import { TaskCreateComponent } from './components/tasks/task-create/task-create.component';
import { JobListComponent } from './components/jobs/job-list/job-list.component';
import { JobCreateComponent } from './components/jobs/job-create/job-create.component';
import { ROUTES, ROUTE_PARAMS } from './constants';

const routes: Routes = [
  {
    path: ROUTES.ROOT,
    redirectTo: ROUTES.DASHBOARD,
    pathMatch: ROUTE_PARAMS.PATH_MATCH_FULL,
  },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'tasks', component: TaskListComponent },
  { path: 'tasks/create', component: TaskCreateComponent },
  { path: 'jobs', component: JobListComponent },
  { path: 'jobs/create', component: JobCreateComponent },
  { path: ROUTES.WILDCARD, redirectTo: ROUTES.DASHBOARD },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
