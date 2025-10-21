import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { ReportService } from './report.service';
import { ReportDto, ReportStatus } from '../models';
import { REPORTS_ENDPOINTS } from '../constants/endpoints';

describe('ReportService', () => {
  let service: ReportService;
  let httpMock: HttpTestingController;

  const mockReport: ReportDto = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    jobId: '123e4567-e89b-12d3-a456-426614174001',
    fileName: 'test-report.pdf',
    contentType: 'application/pdf',
    createdAt: '2024-01-01T00:00:00Z',
    status: ReportStatus.Generated,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReportService],
    });
    service = TestBed.inject(ReportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('downloadReport', () => {
    it('should download a report as blob', () => {
      const reportId = '123e4567-e89b-12d3-a456-426614174000';
      const mockBlob = new Blob(['test content'], { type: 'application/pdf' });

      service.downloadReport(reportId).subscribe((blob) => {
        expect(blob).toEqual(mockBlob);
        expect(blob.type).toBe('application/pdf');
      });

      const req = httpMock.expectOne(REPORTS_ENDPOINTS.DOWNLOAD(reportId));
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(mockBlob);
    });

    it('should handle error when downloading report', () => {
      const reportId = 'non-existent-id';

      service.downloadReport(reportId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(REPORTS_ENDPOINTS.DOWNLOAD(reportId));
      req.flush(new Blob(['Report not found'], { type: 'text/plain' }), {
        status: 404,
        statusText: 'Not Found',
      });
    });
  });

  describe('getReportByJobId', () => {
    it('should return report info by job ID', () => {
      const jobId = '123e4567-e89b-12d3-a456-426614174001';

      service.getReportByJobId(jobId).subscribe((report) => {
        expect(report).toEqual(mockReport);
        expect(report.jobId).toBe(jobId);
        expect(report.fileName).toBe('test-report.pdf');
        expect(report.status).toBe(ReportStatus.Generated);
      });

      const req = httpMock.expectOne(REPORTS_ENDPOINTS.GET_BY_JOB_ID(jobId));
      expect(req.request.method).toBe('GET');
      req.flush(mockReport);
    });

    it('should handle error when report not found for job', () => {
      const jobId = 'invalid-job-id';

      service.getReportByJobId(jobId).subscribe({
        next: () => fail('Expected an error, not a successful response'),
        error: (error) => {
          expect(error.status).toBe(404);
          expect(error.statusText).toBe('Not Found');
        },
      });

      const req = httpMock.expectOne(REPORTS_ENDPOINTS.GET_BY_JOB_ID(jobId));
      req.flush('Report not found for job', {
        status: 404,
        statusText: 'Not Found',
      });
    });
  });

  describe('triggerDownload', () => {
    let createElementSpy: jasmine.Spy;
    let appendChildSpy: jasmine.Spy;
    let removeChildSpy: jasmine.Spy;
    let createObjectURLSpy: jasmine.Spy;
    let revokeObjectURLSpy: jasmine.Spy;
    let mockLink: HTMLAnchorElement;

    beforeEach(() => {
      mockLink = document.createElement('a');
      spyOn(mockLink, 'click');

      createElementSpy = spyOn(document, 'createElement').and.returnValue(
        mockLink
      );
      appendChildSpy = spyOn(document.body, 'appendChild');
      removeChildSpy = spyOn(document.body, 'removeChild');
      createObjectURLSpy = spyOn(window.URL, 'createObjectURL').and.returnValue(
        'blob:https://localhost/test'
      );
      revokeObjectURLSpy = spyOn(window.URL, 'revokeObjectURL');
    });

    it('should trigger file download', () => {
      const blob = new Blob(['test'], { type: 'application/pdf' });
      const fileName = 'test-report.pdf';

      service.triggerDownload(blob, fileName);

      expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
      expect(createElementSpy).toHaveBeenCalledWith('a');
      expect(mockLink.href).toBe('blob:https://localhost/test');
      expect(mockLink.download).toBe(fileName);
      expect(appendChildSpy).toHaveBeenCalledWith(mockLink);
      expect(mockLink.click).toHaveBeenCalled();
      expect(removeChildSpy).toHaveBeenCalledWith(mockLink);
      expect(revokeObjectURLSpy).toHaveBeenCalledWith(
        'blob:https://localhost/test'
      );
    });

    it('should work with different file types', () => {
      const blob = new Blob(['test'], { type: 'text/csv' });
      const fileName = 'data.csv';

      service.triggerDownload(blob, fileName);

      expect(mockLink.download).toBe(fileName);
      expect(mockLink.click).toHaveBeenCalled();
    });
  });

  describe('endpoint construction', () => {
    it('should construct correct endpoint URLs', () => {
      const reportId = '123e4567-e89b-12d3-a456-426614174000';
      const jobId = '123e4567-e89b-12d3-a456-426614174001';

      expect(REPORTS_ENDPOINTS.DOWNLOAD(reportId)).toContain(
        `/reports/${reportId}/download`
      );
      expect(REPORTS_ENDPOINTS.GET_BY_JOB_ID(jobId)).toContain(
        `/reports/by-job/${jobId}`
      );
    });
  });
});
