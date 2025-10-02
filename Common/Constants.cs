namespace WebBoard.Common
{
    public static class Constants
    {
        public static class ApiRoutes
        {
            public const string Tasks = "/api/tasks";
            public const string TaskById = "/api/tasks/{id:guid}";
            public const string Jobs = "/api/jobs";
            public const string JobById = "/api/jobs/{id:guid}";
        }

        public static class JobTypes
        {
            public const string MarkTasksAsCompleted = "MarkTasksAsCompleted";
            public const string GenerateTaskList = "GenerateTaskList";
        }

        public static class SwaggerTags
        {
            public const string Tasks = "Tasks";
            public const string Jobs = "Jobs";
        }

        public static class JobDataKeys
        {
            public const string JobId = "JobId";
        }
    }
}
