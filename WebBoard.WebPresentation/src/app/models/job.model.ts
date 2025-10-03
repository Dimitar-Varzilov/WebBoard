export enum JobStatus {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
}

export interface JobDto {
  id: string;
  jobType: string;
  status: JobStatus;
  createdAt: Date;
}

export interface CreateJobRequestDto {
  jobType: string;
}
