// import { ComponentFixture, TestBed } from '@angular/core/testing';
// import { ReactiveFormsModule } from '@angular/forms';
// import { Router, ActivatedRoute, RouterModule } from '@angular/router';
// import { RouterTestingModule } from '@angular/router/testing';
// import { of, throwError } from 'rxjs';
// import { JobCreateComponent } from './job-create.component';
// import { JobService } from '../../../services';
// import { JOB_TYPES, ROUTES } from '../../../constants';
// import { TaskDtoRaw } from '../../../models/task.model';
// import { JobDtoRaw } from '../../../models/job.model';

// describe('JobCreateComponent', () => {
//   let component: JobCreateComponent;
//   let fixture: ComponentFixture<JobCreateComponent>;
//   let mockJobService: jasmine.SpyObj<JobService>;
//   let mockRouter: jasmine.SpyObj<Router>;
//   let mockActivatedRoute: any;

//   const mockTasks: TaskDtoRaw[] = [
//     {
//       id: 'task-1',
//       title: 'Task 1',
//       description: 'Description 1',
//       status: 0,
//       createdAt: '2025-01-15T10:00:00Z',
//       jobId: undefined,
//     },
//     {
//       id: 'task-2',
//       title: 'Task 2',
//       description: 'Description 2',
//       status: 0,
//       createdAt: '2025-01-15T11:00:00Z',
//       jobId: undefined,
//     },
//   ];

//   const mockCreatedJob: JobDtoRaw = {
//     id: 'job-123',
//     jobType: 'MarkAllTasksAsDone',
//     status: 0,
//     createdAt: '2025-01-15T12:00:00Z',
//     scheduledAt: undefined,
//     hasReport: false,
//     reportId: undefined,
//     reportFileName: undefined,
//     taskIds: ['task-1', 'task-2'],
//   };

//   beforeEach(async () => {
//     mockJobService = jasmine.createSpyObj('JobService', [
//       'getPendingTasksCount',
//       'getAvailableTasksForJob',
//       'createJob',
//     ]);
//     mockRouter = jasmine.createSpyObj('Router', ['navigate']);
//     mockActivatedRoute = {
//       queryParams: of({}),
//     };

//     await TestBed.configureTestingModule({
//       declarations: [JobCreateComponent],
//       imports: [ReactiveFormsModule, RouterTestingModule, RouterModule.forRoot([])],
//       providers: [
//         { provide: JobService, useValue: mockJobService },
//         { provide: Router, useValue: mockRouter },
//         { provide: ActivatedRoute, useValue: mockActivatedRoute },
//       ],
//     }).compileComponents();

//     mockJobService.getPendingTasksCount.and.returnValue(of(5));
//     mockJobService.getAvailableTasksForJob.and.returnValue(of(mockTasks));
//     mockJobService.createJob.and.returnValue(of(mockCreatedJob as any));
//   });

//   beforeEach(() => {
//     fixture = TestBed.createComponent(JobCreateComponent);
//     component = fixture.componentInstance;
//     fixture.detectChanges();
//   });

//   it('should create', () => {
//     expect(component).toBeTruthy();
//   });

//   describe('Initialization', () => {
//     it('should initialize form with default values', () => {
//       expect(component.jobForm.get('jobType')?.value).toBe('');
//       expect(component.jobForm.get('runImmediately')?.value).toBe(true);
//       expect(component.jobForm.get('scheduledAt')?.disabled).toBe(true);
//       expect(component.jobForm.get('taskIds')?.value).toEqual([]);
//     });

//     it('should load pending tasks count on init', () => {
//       expect(mockJobService.getPendingTasksCount).toHaveBeenCalled();
//       expect(component.pendingTasksCount).toBe(5);
//     });

//     it('should populate available job types', () => {
//       expect(component.availableJobTypes.length).toBeGreaterThan(0);
//       expect(component.availableJobTypes[0].value).toBeTruthy();
//       expect(component.availableJobTypes[0].label).toBeTruthy();
//     });
//   });

//   describe('Job Type Selection', () => {
//     it('should load available tasks when job type is selected', () => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

//       expect(mockJobService.getAvailableTasksForJob).toHaveBeenCalledWith(
//         JOB_TYPES.MARK_ALL_TASKS_DONE
//       );
//       expect(component.availableTasks.length).toBe(2);
//     });

//     it('should reset task selection when job type changes', () => {
//       component.jobForm.get('taskIds')?.setValue(['task-1']);
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

//       expect(component.jobForm.get('taskIds')?.value).toEqual([]);
//     });

//     it('should show job type description when selected', () => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

//       expect(component.selectedJobTypeDescription).toBeTruthy();
//     });
//   });

//   describe('Scheduling', () => {
//     it('should enable scheduledAt field when runImmediately is false', () => {
//       component.jobForm.get('runImmediately')?.setValue(false);

//       expect(component.jobForm.get('scheduledAt')?.enabled).toBe(true);
//       expect(component.isScheduled).toBe(true);
//     });

//     it('should disable and clear scheduledAt field when runImmediately is true', () => {
//       component.jobForm.get('runImmediately')?.setValue(false);
//       component.jobForm.get('scheduledAt')?.setValue('2025-01-20T15:00');
//       component.jobForm.get('runImmediately')?.setValue(true);

//       expect(component.jobForm.get('scheduledAt')?.disabled).toBe(true);
//     });

//     it('should require scheduledAt when runImmediately is false', () => {
//       component.jobForm.get('runImmediately')?.setValue(false);

//       const scheduledAtControl = component.jobForm.get('scheduledAt');
//       expect(scheduledAtControl?.hasError('required')).toBe(true);
//     });

//     it('should provide minimum datetime for input', () => {
//       const minDateTime = component.minDateTime;

//       expect(minDateTime).toBeTruthy();
//       expect(minDateTime).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/);
//     });

//     it('should provide timezone information', () => {
//       const timezoneInfo = component.timezoneInfo;

//       expect(timezoneInfo).toBeTruthy();
//       expect(timezoneInfo).toContain('(');
//       expect(timezoneInfo).toContain(')');
//     });
//   });

//   describe('Task Selection', () => {
//     beforeEach(() => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       fixture.detectChanges();
//     });

//     it('should toggle task selection', () => {
//       component.toggleTaskSelection('task-1');
//       expect(component.isTaskSelected('task-1')).toBe(true);

//       component.toggleTaskSelection('task-1');
//       expect(component.isTaskSelected('task-1')).toBe(false);
//     });

//     it('should select all tasks', () => {
//       component.selectAllTasks();

//       expect(component.selectedTaskIds.length).toBe(2);
//       expect(component.selectedTaskIds).toContain('task-1');
//       expect(component.selectedTaskIds).toContain('task-2');
//     });

//     it('should clear task selection', () => {
//       component.jobForm.get('taskIds')?.setValue(['task-1', 'task-2']);
//       component.clearTaskSelection();

//       expect(component.selectedTaskIds.length).toBe(0);
//     });

//     it('should track selected tasks count', () => {
//       component.toggleTaskSelection('task-1');
//       expect(component.selectedTasksCount).toBe(1);

//       component.toggleTaskSelection('task-2');
//       expect(component.selectedTasksCount).toBe(2);
//     });
//   });

//   describe('Task Search', () => {
//     beforeEach(() => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       fixture.detectChanges();
//     });

//     it('should filter tasks by title', (done) => {
//       component.jobForm.get('taskSearchText')?.setValue('Task 1');

//       setTimeout(() => {
//         expect(component.filteredTasks.length).toBe(1);
//         expect(component.filteredTasks[0].title).toBe('Task 1');
//         done();
//       }, 350); // Wait for debounce
//     });

//     it('should filter tasks by description', (done) => {
//       component.jobForm.get('taskSearchText')?.setValue('Description 2');

//       setTimeout(() => {
//         expect(component.filteredTasks.length).toBe(1);
//         expect(component.filteredTasks[0].description).toBe('Description 2');
//         done();
//       }, 350);
//     });

//     it('should show all tasks when search is cleared', (done) => {
//       component.jobForm.get('taskSearchText')?.setValue('Task 1');
//       setTimeout(() => {
//         component.jobForm.get('taskSearchText')?.setValue('');
//         setTimeout(() => {
//           expect(component.filteredTasks.length).toBe(2);
//           done();
//         }, 350);
//       }, 350);
//     });
//   });

//   describe('Form Validation', () => {
//     it('should require job type', () => {
//       expect(component.jobForm.get('jobType')?.hasError('required')).toBe(true);
//       expect(component.canCreateJob).toBe(false);
//     });

//     it('should require at least one task', () => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

//       expect(component.canCreateJob).toBe(false);
//       expect(component.validationMessage).toContain('at least one task');
//     });

//     it('should validate successfully with job type and tasks', () => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       component.jobForm.get('taskIds')?.setValue(['task-1']);

//       expect(component.canCreateJob).toBe(true);
//     });

//     it('should show appropriate validation message when no tasks available', () => {
//       mockJobService.getAvailableTasksForJob.and.returnValue(of([]));
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       fixture.detectChanges();

//       expect(component.validationMessage).toContain(
//         'No pending tasks available'
//       );
//     });

//     it('should show loading message while loading tasks', () => {
//       component.loadingTasks = true;
//       expect(component.validationMessage).toBe('Loading available tasks...');
//     });
//   });

//   describe('Job Creation', () => {
//     beforeEach(() => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       component.jobForm.get('taskIds')?.setValue(['task-1', 'task-2']);
//     });

//     it('should create job with immediate execution', () => {
//       component.onSubmit();

//       expect(mockJobService.createJob).toHaveBeenCalledWith(
//         jasmine.objectContaining({
//           jobType: JOB_TYPES.MARK_ALL_TASKS_DONE,
//           runImmediately: true,
//           taskIds: ['task-1', 'task-2'],
//         })
//       );
//     });

//     it('should create job with scheduled execution', () => {
//       // Set all required fields
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       component.jobForm.get('taskIds')?.setValue(['task-1', 'task-2']);
//       component.jobForm.get('runImmediately')?.setValue(false);

//       // Use a far future date to ensure it passes minimumFutureTimeValidator
//       const futureDate = new Date();
//       futureDate.setDate(futureDate.getDate() + 7); // 7 days from now
//       const futureDateString = futureDate.toISOString().slice(0, 16); // Format: YYYY-MM-DDTHH:mm
//       component.jobForm.get('scheduledAt')?.setValue(futureDateString);

//       // Ensure form is valid
//       component.jobForm.updateValueAndValidity();
//       fixture.detectChanges();

//       // Debug: Check if form is valid
//       if (!component.jobForm.valid) {
//         console.log('Form errors:', component.jobForm.errors);
//         Object.keys(component.jobForm.controls).forEach((key) => {
//           const control = component.jobForm.get(key);
//           if (control?.errors) {
//             console.log(`${key} errors:`, control.errors);
//           }
//         });
//       }

//       component.onSubmit();

//       expect(mockJobService.createJob).toHaveBeenCalled();
//       const callArgs = mockJobService.createJob.calls.mostRecent().args[0];
//       expect(callArgs.scheduledAt).toBeTruthy();
//       expect(callArgs.runImmediately).toBe(false);
//     });
//     it('should navigate to jobs list on successful creation', () => {
//       component.onSubmit();

//       expect(mockRouter.navigate).toHaveBeenCalledWith([ROUTES.JOBS], {
//         queryParams: { created: 'job-123' },
//       });
//     });

//     it('should show error alert on creation failure', () => {
//       spyOn(window, 'alert');
//       mockJobService.createJob.and.returnValue(
//         throwError(() => ({ status: 500 }))
//       );

//       component.onSubmit();

//       expect(window.alert).toHaveBeenCalledWith(
//         'Failed to create job. Please try again.'
//       );
//     });

//     it('should handle validation error for no pending tasks', () => {
//       spyOn(window, 'alert');
//       mockJobService.createJob.and.returnValue(
//         throwError(() => ({
//           status: 400,
//           error: { message: 'No pending tasks available' },
//         }))
//       );

//       component.onSubmit();

//       expect(window.alert).toHaveBeenCalled();
//       const alertMessage = (window.alert as jasmine.Spy).calls.mostRecent()
//         .args[0];
//       expect(alertMessage).toContain('No pending tasks');
//     });

//     it('should handle validation error for past scheduled time', () => {
//       spyOn(window, 'alert');
//       mockJobService.createJob.and.returnValue(
//         throwError(() => ({
//           status: 400,
//           error: { message: 'Scheduled time is in the past' },
//         }))
//       );

//       component.onSubmit();

//       expect(window.alert).toHaveBeenCalled();
//       const alertMessage = (window.alert as jasmine.Spy).calls.mostRecent()
//         .args[0];
//       expect(alertMessage).toContain('past');
//     });

//     it('should not submit if form is invalid', () => {
//       component.jobForm.get('jobType')?.setValue('');
//       component.onSubmit();

//       expect(mockJobService.createJob).not.toHaveBeenCalled();
//     });

//     it('should not submit if no tasks selected', () => {
//       spyOn(window, 'alert');
//       // Set job type to make form partially valid
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       // Set empty task selection - this makes form invalid
//       component.jobForm.get('taskIds')?.setValue([]);
//       // Mark the form as touched to trigger validation
//       component.jobForm.get('taskIds')?.markAsTouched();
//       component.onSubmit();

//       // The form.valid check prevents submission, so we never get to the alert
//       expect(mockJobService.createJob).not.toHaveBeenCalled();
//       // The alert is only shown if form.valid is true but taskIds is empty
//       // Since taskIds has required validator, form.valid will be false
//       // So we shouldn't expect the alert to be called
//     });

//     it('should set creating flag during submission', () => {
//       expect(component.creating).toBe(false);

//       component.onSubmit();

//       expect(component.creating).toBe(false); // Synchronously completes in test
//     });
//   });

//   describe('Refresh Functionality', () => {
//     it('should reload available tasks', () => {
//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);
//       mockJobService.getAvailableTasksForJob.calls.reset();

//       component.refreshAvailableTasks();

//       expect(mockJobService.getAvailableTasksForJob).toHaveBeenCalled();
//     });

//     it('should reload pending tasks count from query params', () => {
//       mockActivatedRoute.queryParams = of({ refreshTasks: 'true' });
//       mockJobService.getPendingTasksCount.calls.reset();

//       component.ngOnInit();

//       expect(mockJobService.getPendingTasksCount).toHaveBeenCalled();
//     });
//   });

//   describe('Error Handling', () => {
//     it('should handle error loading pending tasks count', () => {
//       mockJobService.getPendingTasksCount.and.returnValue(
//         throwError(() => new Error('Network error'))
//       );

//       component.ngOnInit();

//       expect(component.pendingTasksCount).toBe(0);
//       expect(component.loadingPendingTasksCount).toBe(false);
//     });

//     it('should handle error loading available tasks', () => {
//       mockJobService.getAvailableTasksForJob.and.returnValue(
//         throwError(() => new Error('Network error'))
//       );

//       component.jobForm.get('jobType')?.setValue(JOB_TYPES.MARK_ALL_TASKS_DONE);

//       expect(component.availableTasks).toEqual([]);
//       expect(component.loadingTasks).toBe(false);
//     });
//   });

//   describe('Cleanup', () => {
//     it('should unsubscribe on destroy', () => {
//       spyOn(component['destroy$'], 'next');
//       spyOn(component['destroy$'], 'complete');

//       component.ngOnDestroy();

//       expect(component['destroy$'].next).toHaveBeenCalled();
//       expect(component['destroy$'].complete).toHaveBeenCalled();
//     });
//   });
// });
