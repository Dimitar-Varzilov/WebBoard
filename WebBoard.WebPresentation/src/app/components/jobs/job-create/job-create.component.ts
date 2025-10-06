import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { JobService } from '../../../services';
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
  // GenerateTaskReport can work with all tasks regardless of status
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
    
    // Listen for query parameter changes that might indicate task creation
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
          // Set to 0 to be safe and show validation message
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
          scheduledAtControl?.setValidators([Validators.required]);
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
      return true; // Jobs that don't require pending tasks can always be created
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

  get isScheduled(): boolean {
    return !this.jobForm.get('runImmediately')?.value;
  }

  get minDateTime(): string {
    const now = new Date();
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  }

  private showValidationPopup(): void {
    const message = `Cannot create job: No pending tasks available.\n\n` +
      `"Mark All Tasks as Done" requires at least one pending task to be created.\n` +
      `Please create some tasks first before scheduling this job.`;
    
    alert(message);
  }

  onSubmit(): void {
    if (!this.jobForm.valid) {
      return;
    }

    // Check if job requiring pending tasks can be created
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

    if (!formValue.runImmediately && formValue.scheduledAt) {
      createRequest.scheduledAt = new Date(formValue.scheduledAt);
    }

    this.jobService.createJob(createRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (job) => {
          this.creating = false;
          // Navigate to jobs list and show success message
          this.router.navigate([ROUTES.JOBS], {
            queryParams: { created: job.id },
          });
        },
        error: (error) => {
          console.error('Error creating job:', error);
          this.creating = false;
          
          // Handle specific validation error from backend
          if (error.status === 400 && error.error?.message?.includes('No pending tasks')) {
            this.showValidationPopup();
          } else {
            alert('Failed to create job. Please try again.');
          }
        },
      });
  }
}
