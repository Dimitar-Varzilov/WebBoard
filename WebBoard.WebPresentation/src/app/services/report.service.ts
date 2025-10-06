import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReportDto } from '../models';
import { REPORTS_ENDPOINTS } from '../constants/endpoints';

@Injectable({
  providedIn: 'root',
})
export class ReportService {
  constructor(private http: HttpClient) {}

  /**
   * Download a report by ID
   * @param reportId Report ID
   * @returns Observable<Blob> for file download
   */
  downloadReport(reportId: string): Observable<Blob> {
    return this.http.get(REPORTS_ENDPOINTS.DOWNLOAD(reportId), {
      responseType: 'blob',
    });
  }

  /**
   * Get report information by job ID
   * @param jobId Job ID
   * @returns Observable<ReportDto>
   */
  getReportByJobId(jobId: string): Observable<ReportDto> {
    return this.http.get<ReportDto>(REPORTS_ENDPOINTS.GET_BY_JOB_ID(jobId));
  }

  /**
   * Trigger automatic download of a file
   * @param blob File blob
   * @param fileName File name
   */
  triggerDownload(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }
}
