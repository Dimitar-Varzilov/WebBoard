import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TaskCardComponent } from './task-card.component';
import { TaskDto, TaskItemStatus } from '../../../models';

describe('TaskCardComponent', () => {
  let component: TaskCardComponent;
  let fixture: ComponentFixture<TaskCardComponent>;

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
    await TestBed.configureTestingModule({
      declarations: [TaskCardComponent],
      schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements
    }).compileComponents();

    fixture = TestBed.createComponent(TaskCardComponent);
    component = fixture.componentInstance;
    component.task = mockTask;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Status Display', () => {
    it('should get correct status class for Pending', () => {
      const statusClass = component.getStatusClass(TaskItemStatus.Pending);
      expect(statusClass).toBe('status-pending');
    });

    it('should get correct status class for InProgress', () => {
      const statusClass = component.getStatusClass(TaskItemStatus.InProgress);
      expect(statusClass).toBe('status-in-progress');
    });

    it('should get correct status class for Completed', () => {
      const statusClass = component.getStatusClass(TaskItemStatus.Completed);
      expect(statusClass).toBe('status-completed');
    });

    it('should return default class for unknown status', () => {
      const statusClass = component.getStatusClass(999 as TaskItemStatus);
      expect(statusClass).toBe('status-pending');
    });
  });

  describe('Status Text', () => {
    it('should get correct status text for Pending', () => {
      const statusText = component.getStatusText(TaskItemStatus.Pending);
      expect(statusText).toBe('Pending');
    });

    it('should get correct status text for InProgress', () => {
      const statusText = component.getStatusText(TaskItemStatus.InProgress);
      expect(statusText).toBe('In Progress');
    });

    it('should get correct status text for Completed', () => {
      const statusText = component.getStatusText(TaskItemStatus.Completed);
      expect(statusText).toBe('Completed');
    });

    it('should return default text for unknown status', () => {
      const statusText = component.getStatusText(999 as TaskItemStatus);
      expect(statusText).toBe('Pending');
    });
  });

  describe('Status Icons', () => {
    it('should get correct icon for Pending status', () => {
      const icon = component.getStatusIcon(TaskItemStatus.Pending);
      expect(icon).toBe('fas fa-clock');
    });

    it('should get correct icon for InProgress status', () => {
      const icon = component.getStatusIcon(TaskItemStatus.InProgress);
      expect(icon).toBe('fas fa-spinner');
    });

    it('should get correct icon for Completed status', () => {
      const icon = component.getStatusIcon(TaskItemStatus.Completed);
      expect(icon).toBe('fas fa-check-circle');
    });

    it('should return default icon for unknown status', () => {
      const icon = component.getStatusIcon(999 as TaskItemStatus);
      expect(icon).toBe('fas fa-clock');
    });
  });

  describe('Event Emissions', () => {
    it('should emit edit event when onEdit is called', () => {
      spyOn(component.edit, 'emit');

      component.onEdit();

      expect(component.edit.emit).toHaveBeenCalledWith(component.task);
    });

    it('should emit view event when onView is called', () => {
      spyOn(component.view, 'emit');

      component.onView();

      expect(component.view.emit).toHaveBeenCalledWith(component.task);
    });

    it('should emit delete event when onDelete is called', () => {
      spyOn(component.delete, 'emit');

      component.onDelete();

      expect(component.delete.emit).toHaveBeenCalledWith(component.task);
    });
  });

  describe('Task Status Enum', () => {
    it('should expose TaskItemStatus enum', () => {
      expect(component.TaskItemStatus).toBe(TaskItemStatus);
    });
  });

  describe('Input Binding', () => {
    it('should accept task input', () => {
      const newTask: TaskDto = {
        ...mockTask,
        id: 'task-456',
        title: 'Different Task',
      };

      component.task = newTask;
      fixture.detectChanges();

      expect(component.task).toBe(newTask);
      expect(component.task.title).toBe('Different Task');
    });
  });
});
