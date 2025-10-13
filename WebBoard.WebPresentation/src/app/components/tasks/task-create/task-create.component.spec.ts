import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
import { TaskCreateComponent } from './task-create.component';
import { TaskService } from '../../../services';
import { TaskDto, TaskItemStatus } from '../../../models';
import { of, throwError } from 'rxjs';

describe('TaskCreateComponent', () => {
  let component: TaskCreateComponent;
  let fixture: ComponentFixture<TaskCreateComponent>;
  let mockTaskService: jasmine.SpyObj<TaskService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockLocation: jasmine.SpyObj<Location>;

  const mockTask: TaskDto = {
    id: 'task-123',
    title: 'New Task',
    description: 'Task Description',
    status: TaskItemStatus.Pending,
    createdAt: '2024-01-01T00:00:00Z',
    createdAtDate: new Date('2024-01-01'),
    createdAtDisplay: 'Jan 1, 2024',
    createdAtRelative: '1 day ago',
    createdAtCompact: '1/1/24',
    isRecent: true,
    age: 0,
    isAssignedToJob: false,
  };

  beforeEach(async () => {
    mockTaskService = jasmine.createSpyObj('TaskService', ['createTask']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);
    mockLocation = jasmine.createSpyObj('Location', ['back']);

    await TestBed.configureTestingModule({
      declarations: [TaskCreateComponent],
      imports: [ReactiveFormsModule],
      providers: [
        { provide: TaskService, useValue: mockTaskService },
        { provide: Router, useValue: mockRouter },
        { provide: Location, useValue: mockLocation },
      ],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(TaskCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Initialization', () => {
    it('should initialize form with empty values', () => {
      expect(component.taskForm.get('title')?.value).toBe('');
      expect(component.taskForm.get('description')?.value).toBe('');
    });

    it('should have required validators on title field', () => {
      const titleControl = component.taskForm.get('title');
      expect(titleControl?.hasError('required')).toBe(true);
    });

    it('should have minLength validator on title field', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.setValue('AB');
      expect(titleControl?.hasError('minlength')).toBe(true);
    });

    it('should have required validators on description field', () => {
      const descriptionControl = component.taskForm.get('description');
      expect(descriptionControl?.hasError('required')).toBe(true);
    });

    it('should have minLength validator on description field', () => {
      const descriptionControl = component.taskForm.get('description');
      descriptionControl?.setValue('AB');
      expect(descriptionControl?.hasError('minlength')).toBe(true);
    });
  });

  describe('Form Validation', () => {
    it('should be invalid when empty', () => {
      expect(component.taskForm.valid).toBe(false);
    });

    it('should be valid with correct data', () => {
      component.taskForm.setValue({
        title: 'Valid Title',
        description: 'Valid Description',
      });

      expect(component.taskForm.valid).toBe(true);
    });

    it('should identify invalid fields', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.markAsTouched();

      expect(component.isFieldInvalid('title')).toBe(true);
    });

    it('should not show invalid for untouched fields', () => {
      expect(component.isFieldInvalid('title')).toBe(false);
    });

    it('should get error message for required field', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.markAsTouched();

      const error = component.getFieldError('title');

      expect(error).toBe('Title is required');
    });

    it('should get error message for minLength field', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.setValue('AB');
      titleControl?.markAsTouched();

      const error = component.getFieldError('title');

      expect(error).toBe('Title must be at least 3 characters');
    });
  });

  describe('Task Creation', () => {
    it('should create task with valid data', () => {
      mockTaskService.createTask.and.returnValue(of(mockTask));

      component.taskForm.setValue({
        title: 'New Task',
        description: 'Task Description',
      });

      component.onSubmit();

      expect(mockTaskService.createTask).toHaveBeenCalledWith({
        title: 'New Task',
        description: 'Task Description',
      });
      expect(component.submitting).toBe(false);
      expect(mockLocation.back).toHaveBeenCalled();
    });

    it('should not submit if form is invalid', () => {
      component.onSubmit();

      expect(mockTaskService.createTask).not.toHaveBeenCalled();
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should mark all fields as touched on invalid submit', () => {
      component.onSubmit();

      expect(component.taskForm.get('title')?.touched).toBe(true);
      expect(component.taskForm.get('description')?.touched).toBe(true);
    });

    it('should set submitting flag during creation', () => {
      mockTaskService.createTask.and.returnValue(of(mockTask));

      component.taskForm.setValue({
        title: 'New Task',
        description: 'Task Description',
      });

      expect(component.submitting).toBe(false);

      component.onSubmit();

      // Flag is reset after observable completes
      expect(component.submitting).toBe(false);
    });
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      component.taskForm.setValue({
        title: 'Valid Title',
        description: 'Valid Description',
      });
    });

    it('should handle validation error from backend', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          errors: {
            title: ['Title is required'],
          },
        },
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(component.submitting).toBe(false);
      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task: Title is required'
      );
    });

    it('should handle validation error with message', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          message: 'Invalid task data',
        },
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task: Invalid task data'
      );
    });

    it('should handle validation error with string message', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: 'Task creation failed',
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task: Task creation failed'
      );
    });

    it('should handle multiple validation errors', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          errors: {
            title: ['Title is required', 'Title too short'],
            description: ['Description is required'],
          },
        },
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        jasmine.stringContaining('Title is required')
      );
      expect(window.alert).toHaveBeenCalledWith(
        jasmine.stringContaining('Description is required')
      );
    });

    it('should handle generic error', () => {
      spyOn(window, 'alert');
      const error = {
        status: 500,
        error: 'Server error',
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(component.submitting).toBe(false);
      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task. Please try again.'
      );
    });

    it('should handle network error', () => {
      spyOn(window, 'alert');
      const error = new Error('Network error');

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task. Please try again.'
      );
    });
  });

  describe('Cancel Action', () => {
    it('should navigate back in history on cancel', () => {
      component.onCancel();

      expect(mockLocation.back).toHaveBeenCalled();
    });
  });

  describe('Component Lifecycle', () => {
    it('should complete destroy subject on destroy', () => {
      spyOn(component['destroy$'], 'next');
      spyOn(component['destroy$'], 'complete');

      component.ngOnDestroy();

      expect(component['destroy$'].next).toHaveBeenCalled();
      expect(component['destroy$'].complete).toHaveBeenCalled();
    });
  });
});
