import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TaskService } from '../../../services';
import { CreateTaskRequestDto, TaskItemStatus } from '../../../models';
import { ROUTES, TASK_STATUS_OPTIONS } from '../../../constants';

@Component({
  selector: 'app-task-create',
  templateUrl: './task-create.component.html',
  styleUrls: ['./task-create.component.scss'],
})
export class TaskCreateComponent implements OnInit {
  taskForm: FormGroup;
  submitting = false;
  taskStatusOptions = TASK_STATUS_OPTIONS;

  constructor(
    private fb: FormBuilder,
    private taskService: TaskService,
    private router: Router
  ) {
    this.taskForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      status: [TaskItemStatus.NotStarted, Validators.required],
    });
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.taskForm.valid) {
      this.submitting = true;
      const createRequest: CreateTaskRequestDto = this.taskForm.value;

      this.taskService.createTask(createRequest).subscribe({
        next: (task) => {
          this.submitting = false;
          this.router.navigate([ROUTES.TASKS]);
        },
        error: (error) => {
          console.error('Error creating task:', error);
          this.submitting = false;
          alert('Failed to create task. Please try again.');
        },
      });
    }
  }

  onCancel(): void {
    this.router.navigate([ROUTES.TASKS]);
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
        return `${
          fieldName.charAt(0).toUpperCase() + fieldName.slice(1)
        } must be at least ${
          field.errors['minlength'].requiredLength
        } characters`;
      }
    }
    return '';
  }
}
