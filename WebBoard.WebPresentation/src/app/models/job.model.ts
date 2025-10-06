export enum JobStatus {
  Queued = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
}

export interface JobDto {
  id: string;
  jobType: string;
  status: JobStatus;
  createdAt: Date;
  scheduledAt?: Date;
  hasReport?: boolean;
  reportId?: string;
  reportFileName?: string;
}

export interface CreateJobRequestDto {
  jobType: string;
  runImmediately?: boolean;
  scheduledAt?: Date;
}

export interface ReportDto {
  id: string;
  jobId: string;
  fileName: string;
  contentType: string;
  createdAt: Date;
  status: ReportStatus;
}

export enum ReportStatus {
  Generated = 0,
  Downloaded = 1,
  Expired = 2,
}
