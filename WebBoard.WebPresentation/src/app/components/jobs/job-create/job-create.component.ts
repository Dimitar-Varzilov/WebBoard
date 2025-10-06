import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { JobService } from '../../../services';
import { notInPastValidator, minimumFutureTimeValidator } from '../../../validators/datetime.validators';
import { DateTimeUtils } from '../../../utils/datetime.utils';
import {
  JOB_TYPES,
  JOB_TYPE_LABELS,
  JOB_TYPE_DESCRIPTIONS,
  JobType,
  ROUTES,
} from '../../../constants';

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
  pendingTasksCount = 0;
  loadingPendingTasksCount = false;
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
    });
  }

  ngOnInit(): void {
    this.setupJobTypes();
    this.setupSchedulingWatcher();
    this.loadPendingTasksCount();
    
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
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
    this.jobService.getPendingTasksCount()
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
            minimumFutureTimeValidator(1) // At least 1 minute in future
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

  get selectedJobTypeDescription(): string | null {
    const selectedJobType = this.jobForm.get('jobType')?.value;
    return selectedJobType
      ? JOB_TYPE_DESCRIPTIONS[selectedJobType as JobType]
      : null;
  }

  get requiresPendingTasksSelected(): boolean {
    const selectedJobType = this.jobForm.get('jobType')?.value;
    return this.jobTypesRequiringPendingTasks.includes(selectedJobType);
  }

  get canCreateJob(): boolean {
    if (!this.requiresPendingTasksSelected) {
      return true;
    }
    return this.pendingTasksCount > 0;
  }

  get validationMessage(): string | null {
    if (this.loadingPendingTasksCount) {
      return 'Checking pending tasks...';
    }
    
    if (this.requiresPendingTasksSelected && this.pendingTasksCount === 0) {
      return 'No pending tasks available. "Mark All Tasks as Done" requires at least one pending task to be created.';
    }
    
    return null;
  }

  refreshPendingTasksCount(): void {
    this.loadPendingTasksCount();
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.jobForm.get(fieldName);
    if (field?.errors && field?.touched) {
      if (field.errors['required']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
      }
      if (field.errors['notInPast']) {
        return 'Scheduled time cannot be in the past';
      }
      if (field.errors['minimumFutureTime']) {
        const minMinutes = field.errors['minimumFutureTime'].minimumMinutes;
        return `Scheduled time must be at least ${minMinutes} minute${minMinutes > 1 ? 's' : ''} in the future`;
      }
    }
    return '';
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
    const message = `Cannot create job: No pending tasks available.\n\n` +
      `"Mark All Tasks as Done" requires at least one pending task to be created.\n` +
      `Please create some tasks first before scheduling this job.`;
    
    alert(message);
  }

  onSubmit(): void {
    if (!this.jobForm.valid) {
      this.markFormGroupTouched();
      return;
    }

    if (this.requiresPendingTasksSelected && this.pendingTasksCount === 0) {
      this.showValidationPopup();
      return;
    }

    this.creating = true;

    const formValue = this.jobForm.value;
    const createRequest: any = {
      jobType: formValue.jobType,
      runImmediately: formValue.runImmediately,
    };

    // Convert local datetime to UTC ISO string for API
    if (!formValue.runImmediately && formValue.scheduledAt) {
      createRequest.scheduledAt = DateTimeUtils.localInputToUtcIso(formValue.scheduledAt);
      console.log('Scheduling job for:', {
        localInput: formValue.scheduledAt,
        utcIso: createRequest.scheduledAt,
        timezone: this.timezoneInfo
      });
    }

    this.jobService.createJob(createRequest)
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
          
          if (error.status === 400 && error.error?.message?.includes('No pending tasks')) {
            this.showValidationPopup();
          } else if (error.status === 400 && error.error?.message?.includes('past')) {
            alert('Scheduled time cannot be in the past. Please select a future date and time.');
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
    Object.keys(this.jobForm.controls).forEach(key => {
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
      Object.keys(errorResponse.errors).forEach(key => {
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
