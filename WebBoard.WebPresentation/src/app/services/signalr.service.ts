import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface JobStatusUpdate {
  jobId: string;
  jobType: string;
  status: number;
  updatedAt: string;
  progress?: number;
  errorMessage?: string;
  hasReport?: boolean;
  reportId?: string;
  reportFileName?: string;
  taskCount?: number;
}

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  private hubConnection?: signalR.HubConnection;
  private jobStatusUpdates$ = new BehaviorSubject<JobStatusUpdate | null>(null);
  private connectionState$ = new BehaviorSubject<signalR.HubConnectionState>(
    signalR.HubConnectionState.Disconnected
  );

  constructor() {
    this.initializeConnection();
  }

  private initializeConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.signalRUrl, {
        withCredentials: true,
        transport:
          signalR.HttpTransportType.WebSockets |
          signalR.HttpTransportType.ServerSentEvents,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, then 60s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          if (retryContext.previousRetryCount === 3) return 30000;
          return 60000;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Listen for job status updates
    this.hubConnection.on('JobStatusUpdated', (update: JobStatusUpdate) => {
      console.log('📨 Received job status update:', update);
      this.jobStatusUpdates$.next(update);
    });

    // Listen for job progress updates
    this.hubConnection.on('JobProgressUpdated', (update: any) => {
      console.log('📊 Received job progress update:', update);
      this.jobStatusUpdates$.next(update);
    });

    // Listen for report generation
    this.hubConnection.on('ReportGenerated', (update: JobStatusUpdate) => {
      console.log('📄 Received report generated notification:', update);
      this.jobStatusUpdates$.next(update);
    });

    // Connection state handlers
    this.hubConnection.onreconnecting(() => {
      console.log('🔄 SignalR reconnecting...');
      this.connectionState$.next(signalR.HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(() => {
      console.log('✅ SignalR reconnected');
      this.connectionState$.next(signalR.HubConnectionState.Connected);
    });

    this.hubConnection.onclose((error) => {
      console.log('❌ SignalR connection closed', error);
      this.connectionState$.next(signalR.HubConnectionState.Disconnected);
    });
  }

  async startConnection(): Promise<void> {
    if (!this.hubConnection) return;

    try {
      await this.hubConnection.start();
      console.log('🚀 SignalR connected successfully');
      this.connectionState$.next(signalR.HubConnectionState.Connected);
    } catch (error) {
      console.error('❌ Error starting SignalR connection:', error);
      setTimeout(() => this.startConnection(), 5000); // Retry after 5 seconds
    }
  }

  async stopConnection(): Promise<void> {
    if (!this.hubConnection) return;

    try {
      await this.hubConnection.stop();
      console.log('🛑 SignalR disconnected');
      this.connectionState$.next(signalR.HubConnectionState.Disconnected);
    } catch (error) {
      console.error('❌ Error stopping SignalR connection:', error);
    }
  }

  async subscribeToJob(jobId: string): Promise<void> {
    if (
      !this.hubConnection ||
      this.hubConnection.state !== signalR.HubConnectionState.Connected
    ) {
      console.warn('⚠️ Cannot subscribe - connection not established');
      return;
    }

    try {
      await this.hubConnection.invoke('SubscribeToJob', jobId);
      console.log(`✅ Subscribed to job ${jobId}`);
    } catch (error) {
      console.error(`❌ Error subscribing to job ${jobId}:`, error);
    }
  }

  async unsubscribeFromJob(jobId: string): Promise<void> {
    if (
      !this.hubConnection ||
      this.hubConnection.state !== signalR.HubConnectionState.Connected
    ) {
      return;
    }

    try {
      await this.hubConnection.invoke('UnsubscribeFromJob', jobId);
      console.log(`✅ Unsubscribed from job ${jobId}`);
    } catch (error) {
      console.error(`❌ Error unsubscribing from job ${jobId}:`, error);
    }
  }

  getJobStatusUpdates(): Observable<JobStatusUpdate | null> {
    return this.jobStatusUpdates$.asObservable();
  }

  getConnectionState(): Observable<signalR.HubConnectionState> {
    return this.connectionState$.asObservable();
  }

  isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
