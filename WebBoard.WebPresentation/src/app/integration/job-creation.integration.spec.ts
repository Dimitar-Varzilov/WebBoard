/**
 * End-to-End Integration Tests for Job Creation Workflow
 *
 * These tests verify the complete user journey from loading the job creation page
 * through selecting tasks and creating a job.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { of } from 'rxjs';
import { JobCreateComponent } from '../components/jobs/job-create/job-create.component';
import { JobService } from '../services/job.service';
import { JOB_TYPES, ROUTES } from '../constants';
import { TaskDtoRaw } from '../models/task.model';
import { JobDtoRaw } from '../models/job.model';
import { environment } from '../../environments/environment';
import { ActivatedRoute } from '@angular/router';

describe('E2E: Job Creation Workflow', () => {
  let component: JobCreateComponent;
  let fixture: ComponentFixture<JobCreateComponent>;
  let httpMock: HttpTestingController;
  let router: Router;
  let jobService: JobService;

  const mockTasks: TaskDtoRaw[] = [
    {
      id: 'task-1',
      title: 'Urgent: Fix login bug',
      description: 'Users cannot log in with Google OAuth',
      status: 0,
      createdAt: '2025-01-15T10:00:00Z',
      jobId: undefined,
    },
    {
      id: 'task-2',
      title: 'Update documentation',
      description: 'API documentation needs updating',
      status: 0,
      createdAt: '2025-01-15T11:00:00Z',
      jobId: undefined,
    },
    {
      id: 'task-3',
      title: 'Optimize database queries',
      description: 'Dashboard loading is slow',
      status: 0,
      createdAt: '2025-01-15T12:00:00Z',
      jobId: undefined,
    },
  ];

  beforeEach(async () => {
    const mockActivatedRoute = {
      queryParams: of({}),
    };

    await TestBed.configureTestingModule({
      declarations: [JobCreateComponent],
      imports: [ReactiveFormsModule, HttpClientTestingModule],
      providers: [
        JobService,
        { provide: ActivatedRoute, useValue: mockActivatedRoute },
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    jobService = TestBed.inject(JobService);

    spyOn(router, 'navigate');
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(JobCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Handle initial pending tasks count request
    const pendingCountReq = httpMock.expectOne(
      `${environment.apiUrl}/jobs/validation/pending-tasks-count`
    );
    expect(pendingCountReq.request.method).toBe('GET');
    pendingCountReq.flush(3);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Complete Job Creation Flow', () => {
    it('should complete the entire job creation workflow', (done) => {
      // Step 1: User selects job type
      component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

      // Expect API call to load available tasks
      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      expect(tasksReq.request.method).toBe('GET');
      tasksReq.flush(mockTasks);

      fixture.detectChanges();

      // Step 2: Verify tasks are loaded
      expect(component.availableTasks.length).toBe(3);
      expect(component.filteredTasks.length).toBe(3);

      // Step 3: User searches for specific task
      component.jobForm.get('taskSearchText')?.setValue('login');

      setTimeout(() => {
        fixture.detectChanges();

        // Step 4: Verify search results
        expect(component.filteredTasks.length).toBe(1);
        expect(component.filteredTasks[0].title).toContain('login');

        // Step 5: User selects task
        component.toggleTaskSelection('task-1');
        expect(component.selectedTaskIds).toContain('task-1');
        expect(component.selectedTasksCount).toBe(1);

        // Step 6: User clears search and selects another task
        component.jobForm.get('taskSearchText')?.setValue('');

        setTimeout(() => {
          expect(component.filteredTasks.length).toBe(3);
          component.toggleTaskSelection('task-3');
          expect(component.selectedTasksCount).toBe(2);

          // Step 7: Verify form is valid
          expect(component.canCreateJob).toBe(true);
          expect(component.jobForm.valid).toBe(true);

          // Step 8: User submits the form
          component.onSubmit();

          // Expect API call to create job
          const createReq = httpMock.expectOne(`${environment.apiUrl}/jobs`);
          expect(createReq.request.method).toBe('POST');
          expect(createReq.request.body.jobType).toBe(
            JOB_TYPES.MARK_ALL_TASKS_DONE
          );
          expect(createReq.request.body.taskIds).toEqual(['task-1', 'task-3']);
          expect(createReq.request.body.runImmediately).toBe(true);

          // Step 9: Simulate successful job creation
          const createdJob: JobDtoRaw = {
            id: 'job-123',
            jobType: JOB_TYPES.MARK_ALL_TASKS_DONE,
            status: 0,
            createdAt: '2025-01-15T14:00:00Z',
            scheduledAt: undefined,
            hasReport: false,
            reportId: undefined,
            reportFileName: undefined,
            taskIds: ['task-1', 'task-3'],
          };
          createReq.flush(createdJob);

          fixture.detectChanges();

          // Step 10: Verify navigation to jobs list
          expect(router.navigate).toHaveBeenCalledWith([ROUTES.JOBS], {
            queryParams: { created: 'job-123' },
          });

          done();
        }, 350);
      }, 350);
    });

    it('should handle scheduled job creation', (done) => {
      // User selects job type
      component.jobForm
        .get('jobType')
        ?.setValue(JOB_TYPES.GENERATE_TASK_REPORT);

      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.GENERATE_TASK_REPORT}`
      );
      tasksReq.flush(mockTasks);

      fixture.detectChanges();

      // User opts for scheduled execution
      const futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 7); // 7 days from now
      const futureDateString = futureDate.toISOString().slice(0, 16);
      component.jobForm.get('runImmediately')?.setValue(false);
      component.jobForm.get('scheduledAt')?.setValue(futureDateString);
      component.selectAllTasks();

      fixture.detectChanges();

      // Verify form state
      expect(component.isScheduled).toBe(true);
      expect(component.jobForm.get('scheduledAt')?.enabled).toBe(true);
      expect(component.selectedTasksCount).toBe(3);

      // Debug: Check form validity
      console.log('Form valid:', component.jobForm.valid);
      console.log('Form value:', component.jobForm.value);

      // Submit
      component.onSubmit();
      fixture.detectChanges();

      const createReq = httpMock.expectOne(`${environment.apiUrl}/jobs`);
      expect(createReq.request.body.runImmediately).toBe(false);
      expect(createReq.request.body.scheduledAt).toBeTruthy();
      expect(createReq.request.body.taskIds.length).toBe(3);

      const createdJob: any = {
        id: 'job-456',
        jobType: JOB_TYPES.GENERATE_TASK_REPORT,
        status: 0,
        createdAt: '2025-01-15T14:00:00Z',
        scheduledAt: '2025-01-20T15:00:00Z',
        hasReport: false,
        taskIds: ['task-1', 'task-2', 'task-3'],
      };
      createReq.flush(createdJob);

      expect(router.navigate).toHaveBeenCalled();
      done();
    });

    it('should handle validation errors from backend', (done) => {
      spyOn(window, 'alert');

      component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      tasksReq.flush(mockTasks);

      component.toggleTaskSelection('task-1');
      component.onSubmit();

      const createReq = httpMock.expectOne(`${environment.apiUrl}/jobs`);
      createReq.flush(
        { message: 'No pending tasks available' },
        { status: 400, statusText: 'Bad Request' }
      );

      fixture.detectChanges();

      expect(window.alert).toHaveBeenCalled();
      expect(router.navigate).not.toHaveBeenCalled();
      done();
    });
  });

  describe('Task Selection Workflow', () => {
    beforeEach(() => {
      component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      tasksReq.flush(mockTasks);
      fixture.detectChanges();
    });

    it('should allow selecting and deselecting individual tasks', () => {
      // Select task
      component.toggleTaskSelection('task-1');
      expect(component.isTaskSelected('task-1')).toBe(true);

      // Deselect task
      component.toggleTaskSelection('task-1');
      expect(component.isTaskSelected('task-1')).toBe(false);
    });

    it('should allow selecting all filtered tasks', () => {
      component.selectAllTasks();

      expect(component.selectedTasksCount).toBe(3);
      expect(component.isTaskSelected('task-1')).toBe(true);
      expect(component.isTaskSelected('task-2')).toBe(true);
      expect(component.isTaskSelected('task-3')).toBe(true);
    });

    it('should allow clearing all selections', () => {
      component.selectAllTasks();
      expect(component.selectedTasksCount).toBe(3);

      component.clearTaskSelection();
      expect(component.selectedTasksCount).toBe(0);
    });

    it('should persist selection when searching', (done) => {
      component.toggleTaskSelection('task-1');
      component.toggleTaskSelection('task-2');

      component.jobForm.get('taskSearchText')?.setValue('documentation');

      setTimeout(() => {
        // Only one task matches search
        expect(component.filteredTasks.length).toBe(1);

        // But both tasks are still selected
        expect(component.selectedTasksCount).toBe(2);
        expect(component.isTaskSelected('task-1')).toBe(true);
        expect(component.isTaskSelected('task-2')).toBe(true);

        done();
      }, 350);
    });
  });

  describe('Error Recovery Workflow', () => {
    it('should recover from network error when loading tasks', () => {
      component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      tasksReq.error(new ErrorEvent('Network error'));

      fixture.detectChanges();

      expect(component.availableTasks).toEqual([]);
      expect(component.loadingTasks).toBe(false);

      // User retries by refreshing
      component.refreshAvailableTasks();

      const retryReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      retryReq.flush(mockTasks);

      expect(component.availableTasks.length).toBe(3);
    });

    it('should recover from network error during job creation', (done) => {
      spyOn(window, 'alert');

      component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

      const tasksReq = httpMock.expectOne(
        `${environment.apiUrl}/jobs/validation/available-tasks?jobType=${JOB_TYPES.MARK_ALL_TASKS_DONE}`
      );
      tasksReq.flush(mockTasks);

      component.toggleTaskSelection('task-1');
      component.onSubmit();

      const createReq = httpMock.expectOne(`${environment.apiUrl}/jobs`);
      createReq.error(new ErrorEvent('Network error'));

      fixture.detectChanges();

      // User sees error
      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create job. Please try again.'
      );

      // User is still on the form
      expect(router.navigate).not.toHaveBeenCalled();

      // User can retry
      expect(component.canCreateJob).toBe(true);

      done();
    });
  });
});
