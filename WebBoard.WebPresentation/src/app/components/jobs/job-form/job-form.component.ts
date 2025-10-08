import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { JobDto, UpdateJobRequestDto, JobStatus } from '../../../models';
import { JobService } from '../../../services';

@Component({
  selector: 'app-job-form',
  templateUrl: './job-form.component.html',
  styleUrls: ['./job-form.component.scss'],
})
export class JobFormComponent implements OnInit {
  @Input() job: JobDto | null = null;
  @Input() isEdit = false;
  @Output() save = new EventEmitter<JobDto>();
  @Output() cancel = new EventEmitter<void>();

  jobForm!: FormGroup;
  saving = false;
  JobStatus = JobStatus;

  constructor(private fb: FormBuilder, private jobService: JobService) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  private initializeForm(): void {
    if (this.isEdit && this.job) {
      // Check if job is not queued (read-only)
      if (this.job.status !== JobStatus.Queued) {
        alert('Cannot edit a non-queued job. Only queued jobs can be edited.');
        this.cancel.emit();
        return;
      }

      // Edit mode - only allow editing scheduled time
      this.jobForm = this.fb.group({
        jobType: [{ value: this.job.jobType, disabled: true }], // Read-only
        runImmediately: [!this.job.scheduledAt],
        scheduledAt: [this.job.scheduledAt || ''],
      });

      // Setup scheduling watcher
      this.setupSchedulingWatcher();
    } else {
      // Create mode is not supported in this component
      // Jobs should be created via job-create component
      this.cancel.emit();
    }
  }

  private setupSchedulingWatcher(): void {
    this.jobForm
      .get('runImmediately')
      ?.valueChanges.subscribe((runImmediately) => {
        const scheduledAtControl = this.jobForm.get('scheduledAt');
        if (runImmediately) {
          scheduledAtControl?.setValue('');
          scheduledAtControl?.clearValidators();
        } else {
          scheduledAtControl?.setValidators([Validators.required]);
        }
        scheduledAtControl?.updateValueAndValidity();
      });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.jobForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSubmit(): void {
    if (this.jobForm.valid && this.isEdit && this.job) {
      this.saving = true;

      const updateRequest: UpdateJobRequestDto = {
        jobType: this.job.jobType, // Keep existing job type
        runImmediately: this.jobForm.value.runImmediately,
        scheduledAt: this.jobForm.value.runImmediately
          ? undefined
          : this.jobForm.value.scheduledAt,
        taskIds: this.job.taskIds || [], // Keep existing task IDs
      };

      this.jobService.updateJob(this.job.id, updateRequest).subscribe({
        next: (updatedJob) => {
          this.saving = false;
          this.save.emit(updatedJob);
        },
        error: (error) => {
          console.error('Error updating job:', error);
          this.saving = false;
          if (error.status === 409 || error.status === 400) {
            alert(
              error.error?.message ||
                'Cannot update a non-queued job. Only queued jobs can be edited.'
            );
            this.cancel.emit();
          } else {
            alert('Failed to update job. Please try again.');
          }
        },
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }

  private markFormGroupTouched(): void {
    Object.keys(this.jobForm.controls).forEach((key) => {
      const control = this.jobForm.get(key);
      control?.markAsTouched();
    });
  }
}
