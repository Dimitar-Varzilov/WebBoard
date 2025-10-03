import { TaskItemStatus } from '../models';

export const TASK_STATUS_OPTIONS = [
  { value: TaskItemStatus.NotStarted, label: 'Not Started' },
  { value: TaskItemStatus.InProgress, label: 'In Progress' },
  { value: TaskItemStatus.Completed, label: 'Completed' },
  { value: TaskItemStatus.OnHold, label: 'On Hold' },
] as const;

export const TASK_STATUS_LABELS = {
  [TaskItemStatus.NotStarted]: 'Not Started',
  [TaskItemStatus.InProgress]: 'In Progress',
  [TaskItemStatus.Completed]: 'Completed',
  [TaskItemStatus.OnHold]: 'On Hold',
} as const;
