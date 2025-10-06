import { environment } from '../../environments/environment';
import { TaskItemStatus } from '../models';

export const API_BASE_URL = environment.apiUrl;

// Controller Constants
export const CONTROLLERS = {
  JOBS: 'jobs',
  TASKS: 'tasks',
  REPORTS: 'reports',
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
  GET_BY_STATUS: (status: TaskItemStatus) => buildEndpoint(CONTROLLERS.TASKS, `/status/${status}`),
  CREATE: buildEndpoint(CONTROLLERS.TASKS),
  UPDATE: (id: string) => buildEndpoint(CONTROLLERS.TASKS, `/${id}`),
  DELETE: (id: string) => buildEndpoint(CONTROLLERS.TASKS, `/${id}`),
  GET_COUNT_BY_STATUS: (status: TaskItemStatus) => buildEndpoint(CONTROLLERS.TASKS, `/status/${status}/count`),
} as const;

// Reports Controller Endpoints
export const REPORTS_ENDPOINTS = {
  BASE: buildEndpoint(CONTROLLERS.REPORTS),
  DOWNLOAD: (id: string) => buildEndpoint(CONTROLLERS.REPORTS, `/${id}/download`),
  GET_BY_JOB_ID: (jobId: string) => buildEndpoint(CONTROLLERS.REPORTS, `/by-job/${jobId}`),
} as const;
