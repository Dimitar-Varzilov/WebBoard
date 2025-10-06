import { TaskItemStatus } from '../models';

export const TASK_STATUS_OPTIONS = [
  { value: TaskItemStatus.Pending, label: 'Pending' },
  { value: TaskItemStatus.InProgress, label: 'In Progress' },
  { value: TaskItemStatus.Completed, label: 'Completed' },
] as const;

export const TASK_STATUS_LABELS = {
  [TaskItemStatus.Pending]: 'Pending',
  [TaskItemStatus.InProgress]: 'In Progress',
  [TaskItemStatus.Completed]: 'Completed',
} as const;
