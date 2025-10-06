import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { JobService } from '../../../services';
import {
  notInPastValidator,
  minimumFutureTimeValidator,
} from '../../../validators/datetime.validators';
import { DateTimeUtils } from '../../../utils/datetime.utils';
import {
  JOB_TYPES,
  JOB_TYPE_LABELS,
  JOB_TYPE_DESCRIPTIONS,
  JobType,
  ROUTES,
} from '../../../constants';
import { TaskDtoRaw } from 'src/app/models/task.model';

interface JobTypeOption {
  value: JobType;
  label: string;
}

@Component({
  selector: 'app-job-create',
  templateUrl: './job-create.component.html',
  styleUrls: ['./job-create.component.scss'],
})
export class JobCreateComponent implements OnInit, OnDestroy {
  jobForm: FormGroup;
  creating = false;
  routes = ROUTES;
  availableJobTypes: JobTypeOption[] = [];
  availableTasks: TaskDtoRaw[] = [];
  filteredTasks: TaskDtoRaw[] = [];
  pendingTasksCount = 0;
  loadingPendingTasksCount = false;
  loadingTasks = false;
  taskSearchText = '';
  private destroy$ = new Subject<void>();

  // Only MarkAllTasksAsDone requires pending tasks
  private readonly jobTypesRequiringPendingTasks = [
    JOB_TYPES.MARK_ALL_TASKS_DONE,
  ];

  constructor(
    private fb: FormBuilder,
    private jobService: JobService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.jobForm = this.fb.group({
      jobType: ['', Validators.required],
      runImmediately: [true],
      scheduledAt: [{ value: '', disabled: true }],
      taskIds: [[], [Validators.required, Validators.minLength(1)]],
      taskSearchText: [''],
    });
  }

  ngOnInit(): void {
    this.setupJobTypes();
    this.setupSchedulingWatcher();
    this.setupJobTypeWatcher();
    this.setupTaskSearchWatcher();
    this.loadPendingTasksCount();

    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe((params) => {
        if (params['refreshTasks']) {
          this.loadPendingTasksCount();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadPendingTasksCount(): void {
    this.loadingPendingTasksCount = true;
    this.jobService
      .getPendingTasksCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (count) => {
          this.pendingTasksCount = count;
          this.loadingPendingTasksCount = false;
        },
        error: (error) => {
          console.error('Error loading pending tasks count:', error);
          this.loadingPendingTasksCount = false;
          this.pendingTasksCount = 0;
        },
      });
  }

  private setupSchedulingWatcher(): void {
    this.jobForm
      .get('runImmediately')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((runImmediately) => {
        const scheduledAtControl = this.jobForm.get('scheduledAt');
        if (runImmediately) {
          scheduledAtControl?.disable();
          scheduledAtControl?.clearValidators();
        } else {
          scheduledAtControl?.enable();
          scheduledAtControl?.setValidators([
            Validators.required,
            notInPastValidator(),
            minimumFutureTimeValidator(1), // At least 1 minute in future
          ]);
        }
        scheduledAtControl?.updateValueAndValidity();
      });
  }

  private setupJobTypes(): void {
    this.availableJobTypes = Object.values(JOB_TYPES).map((jobType) => ({
      value: jobType,
      label: JOB_TYPE_LABELS[jobType],
    }));
  }

  private setupJobTypeWatcher(): void {
    this.jobForm
      .get('jobType')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((jobType) => {
        if (jobType) {
          this.loadAvailableTasks();
          // Reset task selection when job type changes
          this.jobForm.get('taskIds')?.setValue([]);
        }
      });
  }

  private setupTaskSearchWatcher(): void {
    this.jobForm
      .get('taskSearchText')
      ?.valueChanges.pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe((searchText) => {
        this.taskSearchText = searchText || '';
        this.filterTasks();
      });
  }

  private loadAvailableTasks(): void {
    const jobType = this.jobForm.get('jobType')?.value;
    if (!jobType) return;

    this.loadingTasks = true;
    this.jobService
      .getAvailableTasksForJob(jobType)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tasks) => {
          this.availableTasks = tasks;
          this.filterTasks();
          this.loadingTasks = false;
        },
        error: (error) => {
          console.error('Error loading available tasks:', error);
          this.loadingTasks = false;
          this.availableTasks = [];
          this.filteredTasks = [];
        },
      });
  }

  private filterTasks(): void {
    if (!this.taskSearchText) {
      this.filteredTasks = [...this.availableTasks];
    } else {
      const searchLower = this.taskSearchText.toLowerCase();
      this.filteredTasks = this.availableTasks.filter(
        (task) =>
          task.title.toLowerCase().includes(searchLower) ||
          task.description.toLowerCase().includes(searchLower)
      );
    }
  }

  get selectedJobTypeDescription(): string | null {
    const selectedJobType = this.jobForm.get('jobType')?.value;
    return selectedJobType
      ? JOB_TYPE_DESCRIPTIONS[selectedJobType as JobType]
      : null;
  }

  get selectedTaskIds(): string[] {
    return this.jobForm.get('taskIds')?.value || [];
  }

  get selectedTasksCount(): number {
    return this.selectedTaskIds.length;
  }

  get requiresPendingTasksSelected(): boolean {
    const selectedJobType = this.jobForm.get('jobType')?.value;
    return this.jobTypesRequiringPendingTasks.includes(selectedJobType);
  }

  get canCreateJob(): boolean {
    return this.jobForm.valid && this.selectedTaskIds.length > 0;
  }

  get validationMessage(): string | null {
    const jobType = this.jobForm.get('jobType')?.value;

    if (this.loadingTasks) {
      return 'Loading available tasks...';
    }

    if (!jobType) {
      return null;
    }

    if (this.availableTasks.length === 0) {
      if (jobType === JOB_TYPES.MARK_ALL_TASKS_DONE) {
        return 'No pending tasks available. "Mark All Tasks as Done" requires pending tasks that are not assigned to other jobs.';
      } else {
        return 'No available tasks found that are not already assigned to other jobs.';
      }
    }

    if (this.selectedTaskIds.length === 0) {
      return 'Please select at least one task to process.';
    }

    return null;
  }

  isTaskSelected(taskId: string): boolean {
    return this.selectedTaskIds.includes(taskId);
  }

  toggleTaskSelection(taskId: string): void {
    const currentSelection = [...this.selectedTaskIds];
    const index = currentSelection.indexOf(taskId);

    if (index > -1) {
      currentSelection.splice(index, 1);
    } else {
      currentSelection.push(taskId);
    }

    this.jobForm.get('taskIds')?.setValue(currentSelection);
  }

  selectAllTasks(): void {
    const allTaskIds = this.filteredTasks.map((task) => task.id);
    this.jobForm.get('taskIds')?.setValue(allTaskIds);
  }

  clearTaskSelection(): void {
    this.jobForm.get('taskIds')?.setValue([]);
  }

  getFieldError(fieldName: string): string {
    const field = this.jobForm.get(fieldName);
    if (field?.errors && field?.touched) {
      if (field.errors['required']) {
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } is required`;
      }
      if (field.errors['minlength']) {
        const minLength = field.errors['minlength'].requiredLength;
        return `At least ${minLength} task${
          minLength > 1 ? 's' : ''
        } must be selected`;
      }
      if (field.errors['notInPast']) {
        return 'Scheduled time cannot be in the past';
      }
      if (field.errors['minimumFutureTime']) {
        const minMinutes = field.errors['minimumFutureTime'].minimumMinutes;
        return `Scheduled time must be at least ${minMinutes} minute${
          minMinutes > 1 ? 's' : ''
        } in the future`;
      }
    }
    return '';
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  refreshAvailableTasks(): void {
    this.loadAvailableTasks();
  }

  get isScheduled(): boolean {
    return !this.jobForm.get('runImmediately')?.value;
  }

  // Use DateTimeUtils for consistent datetime handling
  get minDateTime(): string {
    return DateTimeUtils.getCurrentLocalInput();
  }

  // Get current timezone info for user reference
  get timezoneInfo(): string {
    return `${DateTimeUtils.getCurrentTimezoneName()} (${DateTimeUtils.getCurrentTimezoneOffset()})`;
  }

  private showValidationPopup(): void {
    const message =
      `Cannot create job: No pending tasks available.\n\n` +
      `"Mark All Tasks as Done" requires at least one pending task to be created.\n` +
      `Please create some tasks first before scheduling this job.`;

    alert(message);
  }

  onSubmit(): void {
    if (!this.jobForm.valid) {
      this.markFormGroupTouched();
      return;
    }

    if (this.selectedTaskIds.length === 0) {
      alert('Please select at least one task to process.');
      return;
    }

    this.creating = true;

    const formValue = this.jobForm.value;
    const createRequest: any = {
      jobType: formValue.jobType,
      runImmediately: formValue.runImmediately,
      taskIds: this.selectedTaskIds,
    };

    // Convert local datetime to UTC ISO string for API
    if (!formValue.runImmediately && formValue.scheduledAt) {
      createRequest.scheduledAt = DateTimeUtils.localInputToUtcIso(
        formValue.scheduledAt
      );
      console.log('Scheduling job for:', {
        localInput: formValue.scheduledAt,
        utcIso: createRequest.scheduledAt,
        timezone: this.timezoneInfo,
      });
    }

    this.jobService
      .createJob(createRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (job) => {
          this.creating = false;
          console.log('Job created successfully:', job);
          this.router.navigate([ROUTES.JOBS], {
            queryParams: { created: job.id },
          });
        },
        error: (error) => {
          console.error('Error creating job:', error);
          this.creating = false;

          if (
            error.status === 400 &&
            error.error?.message?.includes('No pending tasks')
          ) {
            this.showValidationPopup();
          } else if (
            error.status === 400 &&
            error.error?.message?.includes('past')
          ) {
            alert(
              'Scheduled time cannot be in the past. Please select a future date and time.'
            );
          } else if (error.status === 400 && error.error) {
            const errorMessage = this.extractValidationErrors(error.error);
            alert(`Failed to create job: ${errorMessage}`);
          } else {
            alert('Failed to create job. Please try again.');
          }
        },
      });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.jobForm.controls).forEach((key) => {
      const control = this.jobForm.get(key);
      control?.markAsTouched();
    });
  }

  private extractValidationErrors(errorResponse: any): string {
    if (typeof errorResponse === 'string') {
      return errorResponse;
    }

    if (errorResponse.message) {
      return errorResponse.message;
    }

    if (errorResponse.errors) {
      const errors: string[] = [];
      Object.keys(errorResponse.errors).forEach((key) => {
        const fieldErrors = errorResponse.errors[key];
        if (Array.isArray(fieldErrors)) {
          errors.push(...fieldErrors);
        } else {
          errors.push(fieldErrors);
        }
      });
      return errors.join(', ');
    }

    return 'Unknown validation error';
  }
}
