import { Component, OnInit } from '@angular/core';
import { JobDto, JobStatus } from '../../../models';
import { JobService } from '../../../services';

@Component({
  selector: 'app-job-list',
  templateUrl: './job-list.component.html',
  styleUrls: ['./job-list.component.scss'],
})
export class JobListComponent implements OnInit {
  jobs: JobDto[] = [];
  loading = false;
  JobStatus = JobStatus;

  // Modal states
  showJobDetail = false;
  selectedJob: JobDto | null = null;

  constructor(private jobService: JobService) {}

  ngOnInit(): void {
    this.loadJobs();
    // Auto-refresh jobs every 5 seconds
    setInterval(() => {
      this.refreshJobs(true); // Silent refresh
    }, 5000);
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

  refreshJobs(silent = false): void {
    if (!silent) {
      this.loadJobs();
    } else {
      // Silent refresh logic would go here
      // For now, just update existing jobs status
      this.jobs.forEach((job) => {
        if (
          job.status === JobStatus.Pending ||
          job.status === JobStatus.Running
        ) {
          this.refreshJobStatus(job, true);
        }
      });
    }
  }

  createMarkAllDoneJob(): void {
    this.jobService.createJob({ jobType: 'MarkAllTasksAsDone' }).subscribe({
      next: (job) => {
        this.jobs.unshift(job);
        // Start monitoring this job
        this.monitorJobProgress(job);
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
        // Start monitoring this job
        this.monitorJobProgress(job);
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

  refreshJobStatus(job: JobDto, silent = false): void {
    this.jobService.getJobById(job.id).subscribe({
      next: (updatedJob) => {
        const index = this.jobs.findIndex((j) => j.id === job.id);
        if (index !== -1) {
          this.jobs[index] = updatedJob;
        }
      },
      error: (error) => {
        if (!silent) {
          console.error('Error refreshing job status:', error);
        }
      },
    });
  }

  private monitorJobProgress(job: JobDto): void {
    const interval = setInterval(() => {
      this.jobService.getJobById(job.id).subscribe({
        next: (updatedJob) => {
          const index = this.jobs.findIndex((j) => j.id === job.id);
          if (index !== -1) {
            this.jobs[index] = updatedJob;

            // Stop monitoring if job is completed or failed
            if (
              updatedJob.status === JobStatus.Completed ||
              updatedJob.status === JobStatus.Failed
            ) {
              clearInterval(interval);
            }
          }
        },
        error: () => {
          clearInterval(interval);
        },
      });
    }, 2000); // Check every 2 seconds
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
