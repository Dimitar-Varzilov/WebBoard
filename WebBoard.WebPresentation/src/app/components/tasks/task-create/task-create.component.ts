import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TaskService } from '../../../services';
import { CreateTaskRequestDto } from '../../../models';
import { ROUTES } from '../../../constants';

@Component({
  selector: 'app-task-create',
  templateUrl: './task-create.component.html',
  styleUrls: ['./task-create.component.scss'],
})
export class TaskCreateComponent implements OnInit, OnDestroy {
  taskForm: FormGroup;
  submitting = false;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private taskService: TaskService,
    private router: Router
  ) {
    this.taskForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(3)]],
      // Status removed - tasks will automatically get Pending status on creation
    });
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(): void {
    if (this.taskForm.valid) {
      this.submitting = true;
      const createRequest: CreateTaskRequestDto = {
        title: this.taskForm.value.title,
        description: this.taskForm.value.description,
      };

      this.taskService
        .createTask(createRequest)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (task) => {
            this.submitting = false;
            this.router.navigate([ROUTES.TASKS]);
          },
          error: (error) => {
            console.error('Error creating task:', error);
            this.submitting = false;

            // Handle validation errors from backend
            if (error.status === 400 && error.error) {
              const errorMessage = this.extractValidationErrors(error.error);
              alert(`Failed to create task: ${errorMessage}`);
            } else {
              alert('Failed to create task. Please try again.');
            }
          },
        });
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.router.navigate([ROUTES.TASKS]);
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.taskForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.taskForm.get(fieldName);
    if (field?.errors && field?.touched) {
      if (field.errors['required']) {
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } is required`;
      }
      if (field.errors['minlength']) {
        const requiredLength = field.errors['minlength'].requiredLength;
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } must be at least ${requiredLength} characters`;
      }
    }
    return '';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.taskForm.controls).forEach((key) => {
      const control = this.taskForm.get(key);
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
