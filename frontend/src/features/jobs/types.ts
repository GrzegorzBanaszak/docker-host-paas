export type JobStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Canceled";

export type JobListItem = {
  id: string;
  repositoryUrl: string;
  branch?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  createdAtUtc: string;
};

export type JobDetails = {
  id: string;
  repositoryUrl: string;
  branch?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  errorMessage?: string | null;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
};

export type JobLog = {
  content: string;
};

export type JobFile = {
  id: string;
  name: string;
  sizeBytes: number;
};

export type JobFileContent = {
  id: string;
  name: string;
  content: string;
};

export type CreateJobInput = {
  repositoryUrl: string;
  branch?: string;
};
