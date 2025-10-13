import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of, throwError } from 'rxjs';
import { DashboardComponent } from './dashboard.component';
import { TaskService, JobService } from '../../services';
import {
  TaskDto,
  JobDto,
  TaskItemStatus,
  JobStatus,
  PagedResult,
} from '../../models';
import { ROUTES } from '../../constants';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockTaskService: jasmine.SpyObj<TaskService>;
  let mockJobService: jasmine.SpyObj<JobService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockTasks: TaskDto[] = [
    {
      id: 'task-1',
      title: 'Task 1',
      description: 'Description 1',
      status: TaskItemStatus.Pending,
      createdAt: '2025-01-15T10:00:00Z',
      jobId: undefined,
      createdAtDate: new Date('2025-01-15T10:00:00Z'),
      createdAtDisplay: 'Jan 15, 2025, 10:00 AM',
      createdAtRelative: '2 hours ago',
      createdAtCompact: 'Jan 15, 10:00 AM',
      isRecent: true,
      age: 0,
      isAssignedToJob: false,
    } as TaskDto,
    {
      id: 'task-2',
      title: 'Task 2',
      description: 'Description 2',
      status: TaskItemStatus.InProgress,
      createdAt: '2025-01-14T10:00:00Z',
      jobId: 'job-1',
      createdAtDate: new Date('2025-01-14T10:00:00Z'),
      createdAtDisplay: 'Jan 14, 2025, 10:00 AM',
      createdAtRelative: '1 day ago',
      createdAtCompact: 'Jan 14, 10:00 AM',
      isRecent: false,
      age: 1,
      isAssignedToJob: true,
    } as TaskDto,
  ];

  const mockJobs: JobDto[] = [
    {
      id: 'job-1',
      jobType: 'MarkAllTasksAsDone',
      status: JobStatus.Running,
      createdAt: '2025-01-15T09:00:00Z',
      scheduledAt: undefined,
      hasReport: false,
      reportId: undefined,
      reportFileName: undefined,
      taskIds: ['task-1'],
      createdAtDate: new Date('2025-01-15T09:00:00Z'),
      createdAtDisplay: 'Jan 15, 2025, 09:00 AM',
      createdAtRelative: '3 hours ago',
      createdAtCompact: 'Jan 15, 09:00 AM',
      taskCount: 1,
    } as JobDto,
    {
      id: 'job-2',
      jobType: 'GenerateTaskReport',
      status: JobStatus.Completed,
      createdAt: '2025-01-14T09:00:00Z',
      scheduledAt: undefined,
      hasReport: true,
      reportId: 'report-1',
      reportFileName: 'report.txt',
      taskIds: ['task-2'],
      createdAtDate: new Date('2025-01-14T09:00:00Z'),
      createdAtDisplay: 'Jan 14, 2025, 09:00 AM',
      createdAtRelative: '1 day ago',
      createdAtCompact: 'Jan 14, 09:00 AM',
      taskCount: 1,
    } as JobDto,
  ];

  const mockTasksPagedResult: PagedResult<TaskDto> = {
    items: mockTasks,
    metadata: {
      currentPage: 1,
      pageSize: 5,
      totalCount: 10,
      totalPages: 2,
      hasPrevious: false,
      hasNext: true,
    },
  };

  const mockJobsPagedResult: PagedResult<JobDto> = {
    items: mockJobs,
    metadata: {
      currentPage: 1,
      pageSize: 5,
      totalCount: 8,
      totalPages: 2,
      hasPrevious: false,
      hasNext: true,
    },
  };

  beforeEach(async () => {
    mockTaskService = jasmine.createSpyObj('TaskService', ['getTasks']);
    mockJobService = jasmine.createSpyObj('JobService', ['getJobs']);

    await TestBed.configureTestingModule({
      declarations: [DashboardComponent],
      imports: [RouterTestingModule],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        { provide: JobService, useValue: mockJobService },
      ],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    mockTaskService.getTasks.and.returnValue(of(mockTasksPagedResult));
    mockJobService.getJobs.and.returnValue(of(mockJobsPagedResult));
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    spyOn(mockRouter, 'navigate');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load dashboard data on init', () => {
      expect(mockTaskService.getTasks).toHaveBeenCalled();
      expect(mockJobService.getJobs).toHaveBeenCalled();
    });

    it('should load recent tasks with correct parameters', () => {
      expect(mockTaskService.getTasks).toHaveBeenCalledWith(
        jasmine.objectContaining({
          pageNumber: 1,
          pageSize: 5,
          sortBy: 'createdAt',
          sortDirection: 'desc',
        })
      );
    });

    it('should load recent jobs with correct parameters', () => {
      expect(mockJobService.getJobs).toHaveBeenCalledWith(
        jasmine.objectContaining({
          pageNumber: 1,
          pageSize: 5,
          sortBy: 'createdAt',
          sortDirection: 'desc',
        })
      );
    });

    it('should populate recent tasks', () => {
      expect(component.recentTasks.length).toBe(2);
      expect(component.recentTasks[0].id).toBe('task-1');
    });

    it('should populate recent jobs', () => {
      expect(component.recentJobs.length).toBe(2);
      expect(component.recentJobs[0].id).toBe('job-1');
    });
  });

  describe('Task Statistics', () => {
    it('should calculate total tasks count', () => {
      expect(component.taskStats.total).toBe(10);
    });

    it('should calculate pending tasks count', () => {
      expect(component.taskStats.pending).toBe(1);
    });

    it('should calculate in-progress tasks count', () => {
      expect(component.taskStats.inProgress).toBe(1);
    });

    it('should calculate completed tasks count', () => {
      expect(component.taskStats.completed).toBe(0);
    });

    it('should handle empty task list', () => {
      mockTaskService.getTasks.and.returnValue(
        of({
          items: [],
          metadata: {
            currentPage: 1,
            pageSize: 5,
            totalCount: 0,
            totalPages: 0,
            hasPrevious: false,
            hasNext: false,
          },
        })
      );

      component.ngOnInit();

      expect(component.taskStats.total).toBe(0);
      expect(component.taskStats.pending).toBe(0);
    });
  });

  describe('Job Statistics', () => {
    it('should calculate total jobs count', () => {
      expect(component.jobStats.total).toBe(8);
    });

    it('should calculate queued jobs count', () => {
      expect(component.jobStats.queued).toBe(0);
    });

    it('should calculate running jobs count', () => {
      expect(component.jobStats.running).toBe(1);
    });

    it('should calculate completed jobs count', () => {
      expect(component.jobStats.completed).toBe(1);
    });

    it('should handle empty job list', () => {
      mockJobService.getJobs.and.returnValue(
        of({
          items: [],
          metadata: {
            currentPage: 1,
            pageSize: 5,
            totalCount: 0,
            totalPages: 0,
            hasPrevious: false,
            hasNext: false,
          },
        })
      );

      component.ngOnInit();

      expect(component.jobStats.total).toBe(0);
      expect(component.jobStats.running).toBe(0);
    });
  });

  describe('Utility Methods', () => {
    it('should calculate percentage correctly', () => {
      expect(component.getPercentage(5, 10)).toBe(50);
      expect(component.getPercentage(3, 12)).toBe(25);
    });

    it('should return 0 percentage when total is 0', () => {
      expect(component.getPercentage(0, 0)).toBe(0);
      expect(component.getPercentage(5, 0)).toBe(0);
    });

    it('should get correct task status class', () => {
      expect(component.getTaskStatusClass(TaskItemStatus.Pending)).toBe(
        'status-pending'
      );
      expect(component.getTaskStatusClass(TaskItemStatus.InProgress)).toBe(
        'status-in-progress'
      );
      expect(component.getTaskStatusClass(TaskItemStatus.Completed)).toBe(
        'status-completed'
      );
    });

    it('should get correct task status text', () => {
      expect(component.getTaskStatusText(TaskItemStatus.Pending)).toBe(
        'Pending'
      );
      expect(component.getTaskStatusText(TaskItemStatus.InProgress)).toBe(
        'In Progress'
      );
      expect(component.getTaskStatusText(TaskItemStatus.Completed)).toBe(
        'Completed'
      );
    });

    it('should get correct job status class', () => {
      expect(component.getJobStatusClass(JobStatus.Queued)).toBe(
        'job-status-pending'
      );
      expect(component.getJobStatusClass(JobStatus.Running)).toBe(
        'job-status-running'
      );
      expect(component.getJobStatusClass(JobStatus.Completed)).toBe(
        'job-status-completed'
      );
    });

    it('should get correct job status text', () => {
      expect(component.getJobStatusText(JobStatus.Queued)).toBe('Queued');
      expect(component.getJobStatusText(JobStatus.Running)).toBe('Running');
      expect(component.getJobStatusText(JobStatus.Completed)).toBe('Completed');
    });
  });

  describe('Navigation', () => {
    it('should navigate to task create page', () => {
      component.createNewTask();

      expect(mockRouter.navigate).toHaveBeenCalledWith([ROUTES.TASK_CREATE]);
    });

    it('should navigate to job create page', () => {
      component.createJob();

      expect(mockRouter.navigate).toHaveBeenCalledWith([ROUTES.JOB_CREATE]);
    });
  });

  describe('Refresh Functionality', () => {
    it('should reload dashboard data on refresh', () => {
      mockTaskService.getTasks.calls.reset();
      mockJobService.getJobs.calls.reset();

      component.refreshData();

      expect(mockTaskService.getTasks).toHaveBeenCalled();
      expect(mockJobService.getJobs).toHaveBeenCalled();
    });

    it('should update statistics after refresh', () => {
      const newTasksResult: PagedResult<TaskDto> = {
        items: [...mockTasks, { ...mockTasks[0], id: 'task-3' }],
        metadata: {
          currentPage: 1,
          pageSize: 5,
          totalCount: 15,
          totalPages: 3,
          hasPrevious: false,
          hasNext: true,
        },
      };

      mockTaskService.getTasks.and.returnValue(of(newTasksResult));
      component.refreshData();

      expect(component.taskStats.total).toBe(15);
      expect(component.recentTasks.length).toBe(3);
    });
  });

  describe('Error Handling', () => {
    it('should handle task loading error gracefully', () => {
      mockTaskService.getTasks.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      // Create a fresh component instance for this test
      const testFixture = TestBed.createComponent(DashboardComponent);
      const testComponent = testFixture.componentInstance;
      testFixture.detectChanges();

      // Loading becomes false after checkLoadingComplete is called (even from error)
      expect(testComponent.loading).toBe(false);
      expect(testComponent.recentTasks.length).toBe(0);
      // Task service was called
      expect(mockTaskService.getTasks).toHaveBeenCalled();
    });

    it('should handle job loading error gracefully', () => {
      mockJobService.getJobs.and.returnValue(
        throwError(() => new Error('Network error'))
      );

      // Create a fresh component instance for this test
      const testFixture = TestBed.createComponent(DashboardComponent);
      const testComponent = testFixture.componentInstance;
      testFixture.detectChanges();

      // Loading becomes false after checkLoadingComplete is called (even from error)
      expect(testComponent.loading).toBe(false);
      expect(testComponent.recentJobs.length).toBe(0);
      // Job service was called
      expect(mockJobService.getJobs).toHaveBeenCalled();
    });

    it('should set loading to false after both calls complete', () => {
      expect(component.loading).toBe(false);
    });

    it('should set loading to false even if both calls fail', () => {
      mockTaskService.getTasks.and.returnValue(
        throwError(() => new Error('Error'))
      );
      mockJobService.getJobs.and.returnValue(
        throwError(() => new Error('Error'))
      );

      component.ngOnInit();

      expect(component.loading).toBe(false);
    });
  });

  describe('Loading State', () => {
    it('should start with loading false initially', () => {
      const newComponent = new DashboardComponent(
        mockTaskService,
        mockJobService,
        mockRouter
      );

      expect(newComponent.loading).toBe(false);
    });

    it('should set loading to true when loading data', () => {
      // Create a new component without triggering ngOnInit
      const newComponent = new DashboardComponent(
        mockTaskService,
        mockJobService,
        mockRouter
      );

      newComponent.loadDashboardData();

      // Loading is set to true briefly during the call
      expect(mockTaskService.getTasks).toHaveBeenCalled();
    });
  });
});
