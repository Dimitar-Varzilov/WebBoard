export enum JobStatus {
  Queued = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
}

/**
 * Raw JobDto from API - exactly matches backend
 */
export interface JobDtoRaw {
  id: string;
  jobType: string;
  status: JobStatus;
  createdAt: string; // ISO 8601 string from API
  scheduledAt?: string; // ISO 8601 string from API
  hasReport?: boolean;
  reportId?: string;
  reportFileName?: string;
}

/**
 * Enhanced JobDto for frontend use with computed properties
 */
export interface JobDto extends JobDtoRaw {
  // Computed Date objects for efficient operations
  readonly createdAtDate: Date;
  readonly scheduledAtDate?: Date;

  // Cached formatted strings (computed once)
  readonly createdAtDisplay: string;
  readonly createdAtRelative: string;
  readonly createdAtCompact: string;
  readonly scheduledAtDisplay?: string;
  readonly scheduledAtRelative?: string;
  readonly scheduledAtCompact?: string;

  // Computed boolean flags
  readonly isScheduledInPast: boolean;
  readonly isOverdue: boolean;
}

export interface CreateJobRequestDto {
  jobType: string;
  runImmediately?: boolean;
  scheduledAt?: string; // ISO 8601 string with timezone info
}

export interface ReportDto {
  id: string;
  jobId: string;
  fileName: string;
  contentType: string;
  createdAt: string; // ISO 8601 string with timezone info
  status: ReportStatus;
}

export enum ReportStatus {
  Generated = 0,
  Downloaded = 1,
  Expired = 2,
}
