import { TestBed } from '@angular/core/testing';
import { SignalRService, JobStatusUpdate } from './signalr.service';
import * as signalR from '@microsoft/signalr';

describe('SignalRService', () => {
  let service: SignalRService;
  let mockHubConnection: jasmine.SpyObj<signalR.HubConnection>;

  beforeEach(() => {
    // Create mock HubConnection
    mockHubConnection = jasmine.createSpyObj<signalR.HubConnection>(
      'HubConnection',
      [
        'start',
        'stop',
        'on',
        'invoke',
        'onreconnecting',
        'onreconnected',
        'onclose',
      ],
      { state: signalR.HubConnectionState.Disconnected }
    );

    // Mock start to resolve successfully
    mockHubConnection.start.and.returnValue(Promise.resolve());
    mockHubConnection.stop.and.returnValue(Promise.resolve());
    mockHubConnection.invoke.and.returnValue(Promise.resolve());

    // Spy on HubConnectionBuilder
    spyOn(signalR.HubConnectionBuilder.prototype, 'withUrl').and.returnValue(
      signalR.HubConnectionBuilder.prototype
    );
    spyOn(
      signalR.HubConnectionBuilder.prototype,
      'withAutomaticReconnect'
    ).and.returnValue(signalR.HubConnectionBuilder.prototype);
    spyOn(
      signalR.HubConnectionBuilder.prototype,
      'configureLogging'
    ).and.returnValue(signalR.HubConnectionBuilder.prototype);
    spyOn(signalR.HubConnectionBuilder.prototype, 'build').and.returnValue(
      mockHubConnection
    );

    TestBed.configureTestingModule({
      providers: [SignalRService],
    });
    service = TestBed.inject(SignalRService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize hub connection on creation', () => {
    expect(signalR.HubConnectionBuilder.prototype.withUrl).toHaveBeenCalled();
    expect(
      signalR.HubConnectionBuilder.prototype.withAutomaticReconnect
    ).toHaveBeenCalled();
    expect(signalR.HubConnectionBuilder.prototype.build).toHaveBeenCalled();
  });

  describe('startConnection', () => {
    it('should start the hub connection', async () => {
      await service.startConnection();
      expect(mockHubConnection.start).toHaveBeenCalled();
    });

    it('should update connection state on successful start', async () => {
      let connectionState: signalR.HubConnectionState | undefined;
      service.getConnectionState().subscribe((state) => {
        connectionState = state;
      });

      await service.startConnection();

      // Connection state should be updated
      expect(connectionState).toBeDefined();
    });

    it('should handle connection failure and log error', async () => {
      mockHubConnection.start.and.returnValue(
        Promise.reject('Connection failed')
      );

      // Spy on console.error to verify error is logged
      spyOn(console, 'error');

      // Start the connection which will fail (but won't reject the promise)
      await service.startConnection();

      // Verify start was called and error was logged
      expect(mockHubConnection.start).toHaveBeenCalled();
      expect(console.error).toHaveBeenCalledWith(
        '❌ Error starting SignalR connection:',
        'Connection failed'
      );
    });
  });

  describe('stopConnection', () => {
    it('should stop the hub connection', async () => {
      await service.stopConnection();
      expect(mockHubConnection.stop).toHaveBeenCalled();
    });
  });

  describe('subscribeToJob', () => {
    it('should invoke SubscribeToJob with jobId when connected', async () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Connected,
        writable: true,
      });

      const jobId = 'test-job-id';
      await service.subscribeToJob(jobId);

      expect(mockHubConnection.invoke).toHaveBeenCalledWith(
        'SubscribeToJob',
        jobId
      );
    });

    it('should not invoke when not connected', async () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Disconnected,
        writable: true,
      });

      const jobId = 'test-job-id';
      await service.subscribeToJob(jobId);

      expect(mockHubConnection.invoke).not.toHaveBeenCalled();
    });
  });

  describe('unsubscribeFromJob', () => {
    it('should invoke UnsubscribeFromJob with jobId when connected', async () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Connected,
        writable: true,
      });

      const jobId = 'test-job-id';
      await service.unsubscribeFromJob(jobId);

      expect(mockHubConnection.invoke).toHaveBeenCalledWith(
        'UnsubscribeFromJob',
        jobId
      );
    });

    it('should not invoke when not connected', async () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Disconnected,
        writable: true,
      });

      const jobId = 'test-job-id';
      await service.unsubscribeFromJob(jobId);

      expect(mockHubConnection.invoke).not.toHaveBeenCalled();
    });
  });

  describe('getJobStatusUpdates', () => {
    it('should return observable of job status updates', (done) => {
      service.getJobStatusUpdates().subscribe((update) => {
        expect(update).toBeNull(); // Initially null
        done();
      });
    });
  });

  describe('getConnectionState', () => {
    it('should return observable of connection state', (done) => {
      service.getConnectionState().subscribe((state) => {
        expect(state).toBe(signalR.HubConnectionState.Disconnected);
        done();
      });
    });
  });

  describe('isConnected', () => {
    it('should return true when connected', () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Connected,
        writable: true,
      });

      expect(service.isConnected()).toBe(true);
    });

    it('should return false when disconnected', () => {
      Object.defineProperty(mockHubConnection, 'state', {
        value: signalR.HubConnectionState.Disconnected,
        writable: true,
      });

      expect(service.isConnected()).toBe(false);
    });
  });
});
