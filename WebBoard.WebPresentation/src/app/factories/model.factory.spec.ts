import { JobModelFactory, TaskModelFactory } from './model.factory';
import { JobDtoRaw, JobDto } from '../models/job.model';
import { TaskDtoRaw, TaskDto } from '../models/task.model';
import { JobStatus, TaskItemStatus } from '../models';

describe('JobModelFactory', () => {
  let mockJobRaw: JobDtoRaw;

  beforeEach(() => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2025-01-15T12:00:00Z'));

    mockJobRaw = {
      id: 'job-123',
      jobType: 'MarkAllTasksAsDone',
      status: JobStatus.Queued,
      createdAt: '2025-01-15T10:00:00Z',
      scheduledAt: '2025-01-15T14:00:00Z',
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-1', 'task-2', 'task-3'],
    };
  });

  afterEach(() => {
    jasmine.clock().uninstall();
  });

  describe('fromApiResponse', () => {
    it('should convert raw job data to enhanced JobDto', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result).toBeTruthy();
      expect(result.id).toBe('job-123');
      expect(result.jobType).toBe('MarkAllTasksAsDone');
      expect(result.status).toBe(JobStatus.Queued);
    });

    it('should compute createdAtDate correctly', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result.createdAtDate).toBeInstanceOf(Date);
      expect(result.createdAtDate.toISOString()).toBe(
        '2025-01-15T10:00:00.000Z'
      );
    });

    it('should compute scheduledAtDate when scheduled time is provided', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result.scheduledAtDate).toBeInstanceOf(Date);
      expect(result.scheduledAtDate?.toISOString()).toBe(
        '2025-01-15T14:00:00.000Z'
      );
    });

    it('should handle missing scheduledAt gracefully', () => {
      const jobWithoutSchedule = { ...mockJobRaw, scheduledAt: undefined };
      const result = JobModelFactory.fromApiResponse(jobWithoutSchedule);

      expect(result.scheduledAtDate).toBeUndefined();
      expect(result.scheduledAtDisplay).toBeUndefined();
      expect(result.scheduledAtRelative).toBeUndefined();
      expect(result.scheduledAtCompact).toBeUndefined();
    });

    it('should compute all display formats', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result.createdAtDisplay).toBeTruthy();
      expect(result.createdAtRelative).toBeTruthy();
      expect(result.createdAtCompact).toBeTruthy();
      expect(result.scheduledAtDisplay).toBeTruthy();
      expect(result.scheduledAtRelative).toBeTruthy();
      expect(result.scheduledAtCompact).toBeTruthy();
    });

    it('should compute isScheduledInPast as false for future scheduled time', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result.isScheduledInPast).toBe(false);
    });

    it('should compute isScheduledInPast as true for past scheduled time', () => {
      const pastJob = {
        ...mockJobRaw,
        scheduledAt: '2025-01-15T08:00:00Z', // 4 hours ago
      };
      const result = JobModelFactory.fromApiResponse(pastJob);

      expect(result.isScheduledInPast).toBe(true);
    });

    it('should compute isOverdue for queued jobs scheduled in past', () => {
      const overdueJob = {
        ...mockJobRaw,
        status: JobStatus.Queued,
        scheduledAt: '2025-01-15T08:00:00Z',
      };
      const result = JobModelFactory.fromApiResponse(overdueJob);

      expect(result.isOverdue).toBe(true);
    });

    it('should not mark running jobs as overdue even if scheduled in past', () => {
      const runningJob = {
        ...mockJobRaw,
        status: JobStatus.Running,
        scheduledAt: '2025-01-15T08:00:00Z',
      };
      const result = JobModelFactory.fromApiResponse(runningJob);

      expect(result.isOverdue).toBe(false);
    });

    it('should compute taskCount from taskIds array', () => {
      const result = JobModelFactory.fromApiResponse(mockJobRaw);

      expect(result.taskCount).toBe(3);
    });

    it('should handle missing taskIds gracefully', () => {
      const jobWithoutTasks = { ...mockJobRaw, taskIds: undefined };
      const result = JobModelFactory.fromApiResponse(jobWithoutTasks);

      expect(result.taskCount).toBe(0);
    });

    it('should preserve report information', () => {
      const jobWithReport = {
        ...mockJobRaw,
        hasReport: true,
        reportId: 'report-456',
        reportFileName: 'task-report.txt',
      };
      const result = JobModelFactory.fromApiResponse(jobWithReport);

      expect(result.hasReport).toBe(true);
      expect(result.reportId).toBe('report-456');
      expect(result.reportFileName).toBe('task-report.txt');
    });
  });

  describe('fromApiResponseArray', () => {
    it('should convert array of raw jobs to enhanced JobDtos', () => {
      const rawJobs: JobDtoRaw[] = [
        mockJobRaw,
        { ...mockJobRaw, id: 'job-456', jobType: 'GenerateTaskReport' },
      ];

      const result = JobModelFactory.fromApiResponseArray(rawJobs);

      expect(result.length).toBe(2);
      expect(result[0].id).toBe('job-123');
      expect(result[1].id).toBe('job-456');
      expect(result[0].createdAtDisplay).toBeTruthy();
      expect(result[1].createdAtDisplay).toBeTruthy();
    });

    it('should handle empty array', () => {
      const result = JobModelFactory.fromApiResponseArray([]);

      expect(result).toEqual([]);
    });
  });

  describe('refreshComputedProperties', () => {
    it('should recompute all properties', () => {
      const job = JobModelFactory.fromApiResponse(mockJobRaw);

      // Advance time by 1 hour
      jasmine.clock().mockDate(new Date('2025-01-15T13:00:00Z'));

      const refreshed = JobModelFactory.refreshComputedProperties(job);

      expect(refreshed.createdAtRelative).toBeTruthy();
      // Relative time should have changed
      expect(refreshed.createdAtRelative).not.toBe(job.createdAtRelative);
    });
  });
});

describe('TaskModelFactory', () => {
  let mockTaskRaw: TaskDtoRaw;

  beforeEach(() => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date('2025-01-15T12:00:00Z'));

    mockTaskRaw = {
      id: 'task-123',
      title: 'Test Task',
      description: 'Test Description',
      status: TaskItemStatus.Pending,
      createdAt: '2025-01-15T10:00:00Z',
      jobId: undefined,
    };
  });

  afterEach(() => {
    jasmine.clock().uninstall();
  });

  describe('fromApiResponse', () => {
    it('should convert raw task data to enhanced TaskDto', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result).toBeTruthy();
      expect(result.id).toBe('task-123');
      expect(result.title).toBe('Test Task');
      expect(result.status).toBe(TaskItemStatus.Pending);
    });

    it('should compute createdAtDate correctly', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result.createdAtDate).toBeInstanceOf(Date);
      expect(result.createdAtDate.toISOString()).toBe(
        '2025-01-15T10:00:00.000Z'
      );
    });

    it('should compute all display formats', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result.createdAtDisplay).toBeTruthy();
      expect(result.createdAtRelative).toBeTruthy();
      expect(result.createdAtCompact).toBeTruthy();
    });

    it('should compute isRecent as true for tasks less than 24 hours old', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result.isRecent).toBe(true);
    });

    it('should compute isRecent as false for tasks older than 24 hours', () => {
      const oldTask = {
        ...mockTaskRaw,
        createdAt: '2025-01-13T10:00:00Z', // 2 days ago
      };
      const result = TaskModelFactory.fromApiResponse(oldTask);

      expect(result.isRecent).toBe(false);
    });

    it('should compute age in days correctly', () => {
      const oldTask = {
        ...mockTaskRaw,
        createdAt: '2025-01-10T12:00:00Z', // 5 days ago
      };
      const result = TaskModelFactory.fromApiResponse(oldTask);

      expect(result.age).toBe(5);
    });

    it('should compute age as 0 for very recent tasks', () => {
      const veryRecentTask = {
        ...mockTaskRaw,
        createdAt: '2025-01-15T11:30:00Z', // 30 minutes ago
      };
      const result = TaskModelFactory.fromApiResponse(veryRecentTask);

      expect(result.age).toBe(0);
    });

    it('should compute isAssignedToJob as false when jobId is undefined', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result.isAssignedToJob).toBe(false);
    });

    it('should compute isAssignedToJob as true when jobId is present', () => {
      const assignedTask = {
        ...mockTaskRaw,
        jobId: 'job-456',
      };
      const result = TaskModelFactory.fromApiResponse(assignedTask);

      expect(result.isAssignedToJob).toBe(true);
      expect(result.jobId).toBe('job-456');
    });

    it('should preserve all original properties', () => {
      const result = TaskModelFactory.fromApiResponse(mockTaskRaw);

      expect(result.title).toBe('Test Task');
      expect(result.description).toBe('Test Description');
      expect(result.status).toBe(TaskItemStatus.Pending);
    });
  });

  describe('fromApiResponseArray', () => {
    it('should convert array of raw tasks to enhanced TaskDtos', () => {
      const rawTasks: TaskDtoRaw[] = [
        mockTaskRaw,
        {
          ...mockTaskRaw,
          id: 'task-456',
          title: 'Another Task',
          jobId: 'job-123',
        },
      ];

      const result = TaskModelFactory.fromApiResponseArray(rawTasks);

      expect(result.length).toBe(2);
      expect(result[0].id).toBe('task-123');
      expect(result[1].id).toBe('task-456');
      expect(result[0].isAssignedToJob).toBe(false);
      expect(result[1].isAssignedToJob).toBe(true);
    });

    it('should handle empty array', () => {
      const result = TaskModelFactory.fromApiResponseArray([]);

      expect(result).toEqual([]);
    });
  });

  describe('refreshComputedProperties', () => {
    it('should recompute all properties', () => {
      const task = TaskModelFactory.fromApiResponse(mockTaskRaw);

      // Advance time by 2 days
      jasmine.clock().mockDate(new Date('2025-01-17T12:00:00Z'));

      const refreshed = TaskModelFactory.refreshComputedProperties(task);

      expect(refreshed.age).toBeGreaterThan(task.age);
      expect(refreshed.isRecent).toBe(false);
    });
  });
});
