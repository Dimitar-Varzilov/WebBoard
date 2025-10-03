export enum TaskItemStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  OnHold = 3,
}

export interface TaskDto {
  id: string;
  title: string;
  description: string;
  status: TaskItemStatus;
  createdAt: Date;
}

export interface CreateTaskRequestDto {
  title: string;
  description: string;
}

export interface UpdateTaskRequestDto {
  title: string;
  description: string;
  status: TaskItemStatus;
}
