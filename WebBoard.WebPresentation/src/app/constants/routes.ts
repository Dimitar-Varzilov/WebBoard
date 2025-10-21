export const ROUTES = {
  // Main pages
  DASHBOARD: 'dashboard',
  TASKS: 'tasks',
  JOBS: 'jobs',

  // Task routes
  TASKS_CREATE: 'tasks/create',
  TASK_CREATE: 'tasks/create',

  // Job routes
  JOBS_CREATE: 'jobs/create',
  JOB_CREATE: 'jobs/create',

  // Special routes
  ROOT: '',
  WILDCARD: '**',
  SIGNIN: 'signin',
  AUTH_CALLBACK: 'auth-callback',
} as const;

export const ROUTE_PARAMS = {
  REDIRECT_TO: 'redirectTo',
  PATH_MATCH: 'pathMatch',
  PATH_MATCH_FULL: 'full',
} as const;
