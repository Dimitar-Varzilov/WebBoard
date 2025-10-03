export const ROUTES = {
  // Main pages
  DASHBOARD: '/dashboard',
  TASKS: '/tasks',
  JOBS: '/jobs',

  // Task routes
  TASK_CREATE: '/tasks/create',

  // Job routes
  JOB_CREATE: '/jobs/create',

  // Special routes
  ROOT: '',
  WILDCARD: '**',
} as const;

export const ROUTE_PARAMS = {
  REDIRECT_TO: 'redirectTo',
  PATH_MATCH: 'pathMatch',
  PATH_MATCH_FULL: 'full',
} as const;
