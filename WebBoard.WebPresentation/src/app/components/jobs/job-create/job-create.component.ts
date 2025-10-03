import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
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
export class JobCreateComponent implements OnInit {
  jobForm: FormGroup;
  creating = false;
  routes = ROUTES;
  availableJobTypes: JobTypeOption[] = [];

  constructor(
    private fb: FormBuilder,
    private jobService: JobService,
    private router: Router
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
  }

  private setupSchedulingWatcher(): void {
    this.jobForm
      .get('runImmediately')
      ?.valueChanges.subscribe((runImmediately) => {
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

  onSubmit(): void {
    if (this.jobForm.valid) {
      this.creating = true;

      const formValue = this.jobForm.value;
      const createRequest: any = {
        jobType: formValue.jobType,
        runImmediately: formValue.runImmediately,
      };

      if (!formValue.runImmediately && formValue.scheduledAt) {
        createRequest.scheduledAt = new Date(formValue.scheduledAt);
      }

      this.jobService.createJob(createRequest).subscribe({
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
          alert('Failed to create job. Please try again.');
        },
      });
    }
  }
}
