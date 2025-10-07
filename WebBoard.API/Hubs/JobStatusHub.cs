using Microsoft.AspNetCore.SignalR;
using WebBoard.API.Common.Constants;

namespace WebBoard.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time job status updates
    /// </summary>
    public class JobStatusHub(ILogger<JobStatusHub> logger) : Hub
    {

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            logger.LogInformation("Client {ConnectionId} connected to JobStatusHub", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            logger.LogInformation("Client {ConnectionId} disconnected from JobStatusHub", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Allow clients to subscribe to specific job updates
        /// </summary>
        /// <param name="jobId">Job ID to subscribe to</param>
        public async Task SubscribeToJob(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SignalRConstants.Groups.GetJobGroup(Guid.Parse(jobId)));
            logger.LogInformation("Client {ConnectionId} subscribed to job {JobId}", Context.ConnectionId, jobId);
        }

        /// <summary>
        /// Allow clients to unsubscribe from specific job updates
        /// </summary>
        /// <param name="jobId">Job ID to unsubscribe from</param>
        public async Task UnsubscribeFromJob(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRConstants.Groups.GetJobGroup(Guid.Parse(jobId)));
            logger.LogInformation("Client {ConnectionId} unsubscribed from job {JobId}", Context.ConnectionId, jobId);
        }

        /// <summary>
        /// Allow clients to subscribe to multiple jobs at once (batch operation)
        /// </summary>
        /// <param name="jobIds">Array of job IDs to subscribe to</param>
        public async Task SubscribeToJobs(string[] jobIds)
        {
            if (jobIds == null || jobIds.Length == 0)
            {
                logger.LogWarning("Client {ConnectionId} attempted to subscribe with empty job list", Context.ConnectionId);
                return;
            }

            var tasks = new List<Task>();
            foreach (var jobId in jobIds)
            {
                try
                {
                    tasks.Add(SubscribeToJob(jobId));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error subscribing to job {JobId} for client {ConnectionId}", jobId, Context.ConnectionId);
                }
            }

            await Task.WhenAll(tasks);
            logger.LogInformation("Client {ConnectionId} subscribed to {JobCount} jobs", Context.ConnectionId, jobIds.Length);
        }

        /// <summary>
        /// Allow clients to unsubscribe from multiple jobs at once (batch operation)
        /// </summary>
        /// <param name="jobIds">Array of job IDs to unsubscribe from</param>
        public async Task UnsubscribeFromJobs(string[] jobIds)
        {
            if (jobIds == null || jobIds.Length == 0)
            {
                logger.LogWarning("Client {ConnectionId} attempted to unsubscribe with empty job list", Context.ConnectionId);
                return;
            }

            var tasks = new List<Task>();
            foreach (var jobId in jobIds)
            {
                try
                {
                    var groupName = SignalRConstants.Groups.GetJobGroup(Guid.Parse(jobId));
                    tasks.Add(UnsubscribeFromJob(jobId));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error unsubscribing from job {JobId} for client {ConnectionId}", jobId, Context.ConnectionId);
                }
            }

            await Task.WhenAll(tasks);
            logger.LogInformation("Client {ConnectionId} unsubscribed from {JobCount} jobs", Context.ConnectionId, jobIds.Length);
        }
    }
}
