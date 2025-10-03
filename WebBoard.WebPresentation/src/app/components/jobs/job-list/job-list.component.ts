import { Component, OnInit, OnDestroy } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { JobService } from '../../../services';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit, OnDestroy {
  jobs: JobDto[] = [];
  loading = false;
  JobStatus = JobStatus;

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    this.loadJobs();
  }

  ngOnDestroy(): void {
    // Component cleanup
  }

  loadJobs(): void {
    this.loading = true;
    this.jobService.getAllJobs().subscribe({
      next: (jobs) => {
        this.jobs = jobs;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading jobs:', error);
        this.loading = false;
      },
    });
  }

  refreshJobs(): void {
    this.loadJobs();
  }

  createMarkAllDoneJob(): void {
    this.jobService.createJob({ jobType: 'MarkAllTasksAsDone' }).subscribe({
      next: (job) => {
        this.jobs.unshift(job);
      },
      error: (error) => {
        console.error('Error creating mark all done job:', error);
        alert('Failed to create job. Please try again.');
      },
    });
  }

  createGenerateReportJob(): void {
    this.jobService.createJob({ jobType: 'GenerateTaskReport' }).subscribe({
      next: (job) => {
        this.jobs.unshift(job);
      },
      error: (error) => {
        console.error('Error creating generate report job:', error);
        alert('Failed to create job. Please try again.');
      },
    });
  }

  viewJob(job: JobDto): void {
    this.selectedJob = job;
    this.showJobDetail = true;
  }

  refreshJobStatus(job: JobDto): void {
    this.jobService.getJobById(job.id).subscribe({
      next: (updatedJob) => {
        const index = this.jobs.findIndex((j) => j.id === job.id);
        if (index !== -1) {
          this.jobs[index] = updatedJob;
        }
      },
      error: (error) => {
        console.error('Error refreshing job status:', error);
      },
    });
  }

  getJobCountByStatus(status: JobStatus): number {
    return this.jobs.filter((job) => job.status === status).length;
  }

  onJobDetailClosed(): void {
    this.showJobDetail = false;
    this.selectedJob = null;
  }

  trackByJobId(index: number, job: JobDto): string {
    return job.id;
  }
}
