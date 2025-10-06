export enum TaskItemStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  NotStarted = 3,
}

/**
 * Raw TaskDto from API - exactly matches backend
 */
export interface TaskDtoRaw {
  id: string;
  title: string;
  description: string;
  status: TaskItemStatus;
  createdAt: string; // ISO 8601 string from API
  jobId?: string; // Job ID if task is assigned to a job
}

/**
 * Enhanced TaskDto for frontend use with computed properties
 */
export interface TaskDto extends TaskDtoRaw {
  // Computed Date object for efficient operations
  readonly createdAtDate: Date;
  
  // Cached formatted strings (computed once)
  readonly createdAtDisplay: string;
  readonly createdAtRelative: string;
  readonly createdAtCompact: string;
  
  // Computed boolean flags
  readonly isRecent: boolean; // Created within last 24 hours
  readonly age: number; // Age in days
  readonly isAssignedToJob: boolean; // Has job assignment
}

export interface CreateTaskRequestDto {
  title: string;
  description: string;
}

export interface UpdateTaskRequestDto {
  title: string;
  description: string;
  status: TaskItemStatus;
}
