import { environment } from '../../environments/environment';

export const API_BASE_URL = environment.apiUrl;

// Controller Constants
export const CONTROLLERS = {
  JOBS: 'jobs',
  TASKS: 'tasks',
} as const;

// Helper function to build endpoint URLs
const buildEndpoint = (controller: string, path = '') =>
  `${API_BASE_URL}/${controller}${path}`;

// Jobs Controller Endpoints
export const JOBS_ENDPOINTS = {
  BASE: buildEndpoint(CONTROLLERS.JOBS),
  GET_ALL: buildEndpoint(CONTROLLERS.JOBS),
  GET_BY_ID: (id: string) => buildEndpoint(CONTROLLERS.JOBS, `/${id}`),
  CREATE: buildEndpoint(CONTROLLERS.JOBS),
  GET_PENDING_TASKS_COUNT: buildEndpoint(CONTROLLERS.JOBS, '/validation/pending-tasks-count'),
} as const;

// Tasks Controller Endpoints
export const TASKS_ENDPOINTS = {
  BASE: buildEndpoint(CONTROLLERS.TASKS),
  GET_ALL: buildEndpoint(CONTROLLERS.TASKS),
  GET_BY_ID: (id: string) => buildEndpoint(CONTROLLERS.TASKS, `/${id}`),
  CREATE: buildEndpoint(CONTROLLERS.TASKS),
  UPDATE: (id: string) => buildEndpoint(CONTROLLERS.TASKS, `/${id}`),
  DELETE: (id: string) => buildEndpoint(CONTROLLERS.TASKS, `/${id}`),
  GET_COUNT_BY_STATUS: (status: string) => buildEndpoint(CONTROLLERS.TASKS, `/status/${status}/count`),
} as const;
