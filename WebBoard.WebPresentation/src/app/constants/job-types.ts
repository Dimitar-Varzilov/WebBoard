export const JOB_TYPES = {
  MARK_ALL_TASKS_DONE: 'MarkAllTasksAsDone',
  GENERATE_TASK_REPORT: 'GenerateTaskReport',
} as const;

export const JOB_TYPE_LABELS = {
  [JOB_TYPES.MARK_ALL_TASKS_DONE]: 'Mark All Tasks as Done',
  [JOB_TYPES.GENERATE_TASK_REPORT]: 'Generate Task Report',
} as const;

export const JOB_TYPE_DESCRIPTIONS = {
  [JOB_TYPES.MARK_ALL_TASKS_DONE]: 'Mark all pending tasks as completed (requires at least 1 pending task)',
  [JOB_TYPES.GENERATE_TASK_REPORT]: 'Generate a text file report with all tasks (works with any tasks regardless of status)',
} as const;

export type JobType = (typeof JOB_TYPES)[keyof typeof JOB_TYPES];
