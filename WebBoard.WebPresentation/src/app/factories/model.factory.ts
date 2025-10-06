import { JobDto, JobDtoRaw } from '../models/job.model';
import { TaskDto, TaskDtoRaw } from '../models/task.model';
import { DateTimeUtils } from '../utils/datetime.utils';

/**
 * Factory to convert raw API data to enhanced frontend models
 * Computes all date-related properties once for efficient template usage
 */
export class JobModelFactory {
  /**
   * Convert raw JobDto from API to enhanced JobDto with computed properties
   */
  static fromApiResponse(raw: JobDtoRaw): JobDto {
    // Parse dates once
    const createdAtDate = new Date(raw.createdAt);
    const scheduledAtDate = raw.scheduledAt ? new Date(raw.scheduledAt) : undefined;
    
    // Compute all formatted strings once
    const createdAtDisplay = DateTimeUtils.formatForDisplay(raw.createdAt);
    const createdAtRelative = DateTimeUtils.formatRelative(raw.createdAt);
    const createdAtCompact = DateTimeUtils.formatCompact(raw.createdAt);
    
    const scheduledAtDisplay = raw.scheduledAt ? DateTimeUtils.formatForDisplay(raw.scheduledAt) : undefined;
    const scheduledAtRelative = raw.scheduledAt ? DateTimeUtils.formatRelative(raw.scheduledAt) : undefined;
    const scheduledAtCompact = raw.scheduledAt ? DateTimeUtils.formatCompact(raw.scheduledAt) : undefined;
    
    // Compute boolean flags once
    const isScheduledInPast = raw.scheduledAt ? DateTimeUtils.isPast(raw.scheduledAt) : false;
    const isOverdue = isScheduledInPast && raw.status === 0; // Queued but scheduled in past
    
    return {
      ...raw,
      createdAtDate,
      scheduledAtDate,
      createdAtDisplay,
      createdAtRelative,
      createdAtCompact,
      scheduledAtDisplay,
      scheduledAtRelative,
      scheduledAtCompact,
      isScheduledInPast,
      isOverdue
    };
  }

  /**
   * Convert array of raw JobDtos to enhanced JobDtos
   */
  static fromApiResponseArray(rawJobs: JobDtoRaw[]): JobDto[] {
    return rawJobs.map(job => this.fromApiResponse(job));
  }

  /**
   * Refresh computed properties (useful for periodic updates)
   */
  static refreshComputedProperties(job: JobDto): JobDto {
    return this.fromApiResponse(job);
  }
}

/**
 * Factory for Task models with computed properties
 */
export class TaskModelFactory {
  /**
   * Convert raw TaskDto from API to enhanced TaskDto with computed properties
   */
  static fromApiResponse(raw: TaskDtoRaw): TaskDto {
    // Parse date once
    const createdAtDate = new Date(raw.createdAt);
    
    // Compute all formatted strings once
    const createdAtDisplay = DateTimeUtils.formatForDisplay(raw.createdAt);
    const createdAtRelative = DateTimeUtils.formatRelative(raw.createdAt);
    const createdAtCompact = DateTimeUtils.formatCompact(raw.createdAt);
    
    // Compute boolean flags and metrics once
    const now = new Date();
    const ageInMs = now.getTime() - createdAtDate.getTime();
    const ageInDays = Math.floor(ageInMs / (1000 * 60 * 60 * 24));
    const isRecent = ageInMs < (24 * 60 * 60 * 1000); // Less than 24 hours old
    
    return {
      ...raw,
      createdAtDate,
      createdAtDisplay,
      createdAtRelative,
      createdAtCompact,
      isRecent,
      age: ageInDays
    };
  }

  /**
   * Convert array of raw TaskDtos to enhanced TaskDtos
   */
  static fromApiResponseArray(rawTasks: TaskDtoRaw[]): TaskDto[] {
    return rawTasks.map(task => this.fromApiResponse(task));
  }

  /**
   * Refresh computed properties (useful for periodic updates)
   */
  static refreshComputedProperties(task: TaskDto): TaskDto {
    return this.fromApiResponse(task);
  }
}
