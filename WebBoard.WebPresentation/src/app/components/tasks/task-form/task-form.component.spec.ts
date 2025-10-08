import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { TaskFormComponent } from './task-form.component';
import { TaskService } from '../../../services';
import { TaskDto, TaskItemStatus } from '../../../models';
import { of, throwError } from 'rxjs';

describe('TaskFormComponent', () => {
  let component: TaskFormComponent;
  let fixture: ComponentFixture<TaskFormComponent>;
  let mockTaskService: jasmine.SpyObj<TaskService>;

  const mockTask: TaskDto = {
    id: 'task-123',
    title: 'Test Task',
    description: 'Test Description',
    status: TaskItemStatus.Pending,
    createdAt: '2024-01-01T00:00:00Z',
    createdAtDate: new Date('2024-01-01'),
    createdAtDisplay: 'Jan 1, 2024',
    createdAtRelative: '1 day ago',
    createdAtCompact: '1/1/24',
    isRecent: false,
    age: 1,
    isAssignedToJob: false,
  };

  beforeEach(async () => {
    mockTaskService = jasmine.createSpyObj('TaskService', [
      'createTask',
      'updateTask',
    ]);

    await TestBed.configureTestingModule({
      declarations: [TaskFormComponent],
      imports: [ReactiveFormsModule],
      providers: [{ provide: TaskService, useValue: mockTaskService }],
    }).compileComponents();

    fixture = TestBed.createComponent(TaskFormComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Initialization - Create Mode', () => {
    beforeEach(() => {
      component.isEdit = false;
      component.task = null;
      fixture.detectChanges();
    });

    it('should initialize form with empty values', () => {
      expect(component.taskForm.get('title')?.value).toBe('');
      expect(component.taskForm.get('description')?.value).toBe('');
      expect(component.taskForm.get('status')).toBeNull();
    });

    it('should not include status field in create mode', () => {
      expect(component.taskForm.get('status')).toBeNull();
    });

    it('should have required validators', () => {
      const titleControl = component.taskForm.get('title');
      const descControl = component.taskForm.get('description');

      expect(titleControl?.hasError('required')).toBe(true);
      expect(descControl?.hasError('required')).toBe(true);
    });
  });

  describe('Form Initialization - Edit Mode', () => {
    beforeEach(() => {
      component.isEdit = true;
      component.task = mockTask;
      fixture.detectChanges();
    });

    it('should initialize form with task values', () => {
      expect(component.taskForm.get('title')?.value).toBe(mockTask.title);
      expect(component.taskForm.get('description')?.value).toBe(
        mockTask.description
      );
      expect(component.taskForm.get('status')?.value).toBe(mockTask.status);
    });

    it('should include status field in edit mode', () => {
      expect(component.taskForm.get('status')).toBeTruthy();
    });

    it('should mark status field as required in edit mode', () => {
      const statusControl = component.taskForm.get('status');
      statusControl?.setValue('');

      expect(statusControl?.hasError('required')).toBe(true);
    });
  });

  describe('Form Validation', () => {
    beforeEach(() => {
      component.isEdit = false;
      fixture.detectChanges();
    });

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

    it('should validate minLength on title', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.setValue('AB');

      expect(titleControl?.hasError('minlength')).toBe(true);
    });

    it('should validate minLength on description', () => {
      const descControl = component.taskForm.get('description');
      descControl?.setValue('AB');

      expect(descControl?.hasError('minlength')).toBe(true);
    });

    it('should identify invalid fields correctly', () => {
      const titleControl = component.taskForm.get('title');
      titleControl?.markAsTouched();

      expect(component.isFieldInvalid('title')).toBe(true);
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
    beforeEach(() => {
      component.isEdit = false;
      component.task = null;
      fixture.detectChanges();
    });

    it('should create task successfully', () => {
      mockTaskService.createTask.and.returnValue(of(mockTask));
      spyOn(component.save, 'emit');

      component.taskForm.setValue({
        title: 'New Task',
        description: 'New Description',
      });

      component.onSubmit();

      expect(mockTaskService.createTask).toHaveBeenCalledWith({
        title: 'New Task',
        description: 'New Description',
      });
      expect(component.save.emit).toHaveBeenCalledWith(mockTask);
      expect(component.saving).toBe(false);
    });

    it('should not submit if form is invalid', () => {
      component.onSubmit();

      expect(mockTaskService.createTask).not.toHaveBeenCalled();
    });

    it('should mark all fields as touched on invalid submit', () => {
      component.onSubmit();

      expect(component.taskForm.get('title')?.touched).toBe(true);
      expect(component.taskForm.get('description')?.touched).toBe(true);
    });

    it('should set saving flag during creation', () => {
      mockTaskService.createTask.and.returnValue(of(mockTask));

      component.taskForm.setValue({
        title: 'New Task',
        description: 'New Description',
      });

      expect(component.saving).toBe(false);

      component.onSubmit();

      expect(component.saving).toBe(false);
    });
  });

  describe('Task Update', () => {
    beforeEach(() => {
      component.isEdit = true;
      component.task = mockTask;
      fixture.detectChanges();
    });

    it('should update task successfully', () => {
      const updatedTask = { ...mockTask, title: 'Updated Title' };
      mockTaskService.updateTask.and.returnValue(of(updatedTask));
      spyOn(component.save, 'emit');

      component.taskForm.patchValue({
        title: 'Updated Title',
      });

      component.onSubmit();

      expect(mockTaskService.updateTask).toHaveBeenCalledWith(mockTask.id, {
        title: 'Updated Title',
        description: mockTask.description,
        status: mockTask.status,
      });
      expect(component.save.emit).toHaveBeenCalledWith(updatedTask);
      expect(component.saving).toBe(false);
    });

    it('should update task status', () => {
      const updatedTask = { ...mockTask, status: TaskItemStatus.Completed };
      mockTaskService.updateTask.and.returnValue(of(updatedTask));
      spyOn(component.save, 'emit');

      component.taskForm.patchValue({
        status: TaskItemStatus.Completed,
      });

      component.onSubmit();

      expect(mockTaskService.updateTask).toHaveBeenCalledWith(
        mockTask.id,
        jasmine.objectContaining({
          status: TaskItemStatus.Completed,
        })
      );
    });

    it('should not update if form is invalid', () => {
      component.taskForm.patchValue({
        title: '', // Invalid
      });

      component.onSubmit();

      expect(mockTaskService.updateTask).not.toHaveBeenCalled();
    });
  });

  describe('Error Handling - Create', () => {
    beforeEach(() => {
      component.isEdit = false;
      component.task = null;
      fixture.detectChanges();
      component.taskForm.setValue({
        title: 'Valid Title',
        description: 'Valid Description',
      });
    });

    it('should handle validation error with errors object', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          errors: {
            title: ['Title already exists'],
          },
        },
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(component.saving).toBe(false);
      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task: Title already exists'
      );
    });

    it('should handle validation error with message', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          message: 'Invalid data',
        },
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task: Invalid data'
      );
    });

    it('should handle generic error', () => {
      spyOn(window, 'alert');
      const error = {
        status: 500,
      };

      mockTaskService.createTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to create task. Please try again.'
      );
    });
  });

  describe('Error Handling - Update', () => {
    beforeEach(() => {
      component.isEdit = true;
      component.task = mockTask;
      fixture.detectChanges();
    });

    it('should handle update validation error', () => {
      spyOn(window, 'alert');
      const error = {
        status: 400,
        error: {
          message: 'Update failed',
        },
      };

      mockTaskService.updateTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(component.saving).toBe(false);
      expect(window.alert).toHaveBeenCalledWith(
        'Failed to update task: Update failed'
      );
    });

    it('should handle update generic error', () => {
      spyOn(window, 'alert');
      const error = {
        status: 500,
      };

      mockTaskService.updateTask.and.returnValue(throwError(() => error));

      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(
        'Failed to update task. Please try again.'
      );
    });
  });

  describe('Cancel Action', () => {
    it('should emit cancel event', () => {
      spyOn(component.cancel, 'emit');

      component.onCancel();

      expect(component.cancel.emit).toHaveBeenCalled();
    });
  });

  describe('Status Enum Access', () => {
    it('should expose TaskItemStatus enum', () => {
      expect(component.TaskItemStatus).toBe(TaskItemStatus);
    });
  });
});
