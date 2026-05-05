export type JobStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Canceled";
export type ContainerStatus =
  | "running"
  | "restarting"
  | "paused"
  | "exited"
  | "created"
  | "dead"
  | "not_found";

export type JobImageSummary = {
  id: string;
  status: JobStatus;
  detectedStack?: string | null;
  imageTag?: string | null;
  imageId?: string | null;
  sourceCommitSha?: string | null;
  containerPort?: number | null;
  createdAtUtc: string;
  completedAtUtc?: string | null;
  isCurrent: boolean;
};

export type JobListItem = {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  repositoryUrl: string;
  branch?: string | null;
  projectPath?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  containerName?: string | null;
  containerStatus?: ContainerStatus | null;
  containerPort?: number | null;
  publishedPort?: number | null;
  publicAccessEnabled: boolean;
  publicHostname?: string | null;
  routeStatus?: string | null;
  deploymentUrl?: string | null;
  deployedAtUtc?: string | null;
  createdAtUtc: string;
};

export type JobDetails = {
  id: string;
  projectId: string;
  projectName: string;
  name: string;
  repositoryUrl: string;
  branch?: string | null;
  projectPath?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  imageId?: string | null;
  containerId?: string | null;
  containerName?: string | null;
  containerStatus?: ContainerStatus | null;
  containerPort?: number | null;
  publishedPort?: number | null;
  publicAccessEnabled: boolean;
  publicHostname?: string | null;
  routeStatus?: string | null;
  deploymentUrl?: string | null;
  errorMessage?: string | null;
  currentImageId?: string | null;
  currentImage?: JobImageSummary | null;
  images: JobImageSummary[];
  createdAtUtc: string;
  startedAtUtc?: string | null;
  deployedAtUtc?: string | null;
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

export type ImageListItem = {
  id: string;
  jobId: string;
  jobName: string;
  repositoryUrl: string;
  branch?: string | null;
  projectPath?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  imageTag?: string | null;
  imageId?: string | null;
  sourceCommitSha?: string | null;
  containerPort?: number | null;
  createdAtUtc: string;
  completedAtUtc?: string | null;
  isCurrent: boolean;
};

export type ImageDetails = {
  id: string;
  jobId: string;
  jobName: string;
  repositoryUrl: string;
  branch?: string | null;
  projectPath?: string | null;
  jobStatus: JobStatus;
  jobDeploymentUrl?: string | null;
  status: JobStatus;
  detectedStack?: string | null;
  imageTag?: string | null;
  imageId?: string | null;
  sourceCommitSha?: string | null;
  containerPort?: number | null;
  errorMessage?: string | null;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  builtAtUtc?: string | null;
  completedAtUtc?: string | null;
  isCurrent: boolean;
};

export type CreateJobInput = {
  name: string;
  repositoryUrl: string;
  branch?: string;
  projectPath?: string;
  projectId?: string;
};

export type ProjectListItem = {
  id: string;
  name: string;
  repositoryUrl: string;
  defaultBranch?: string | null;
  defaultProjectPath?: string | null;
  currentJobId?: string | null;
  currentImageId?: string | null;
  currentStatus?: JobStatus | null;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  containerName?: string | null;
  containerStatus?: ContainerStatus | null;
  containerPort?: number | null;
  publishedPort?: number | null;
  publicAccessEnabled: boolean;
  publicHostname?: string | null;
  routeStatus?: string | null;
  deploymentUrl?: string | null;
  deployedAtUtc?: string | null;
  jobsCount: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  archivedAtUtc?: string | null;
};

export type ProjectDetails = ProjectListItem & {
  currentImage?: JobImageSummary | null;
  jobs: JobListItem[];
  images: JobImageSummary[];
};

export type CreateProjectInput = {
  name: string;
  repositoryUrl: string;
  defaultBranch?: string;
  defaultProjectPath?: string;
};

export type UpdateProjectInput = CreateProjectInput;

export type CreateProjectJobInput = {
  name?: string;
  branch?: string;
  projectPath?: string;
};

export type RepositoryInspection = {
  branches: string[];
  projectPath?: string | null;
  detectedStack?: string | null;
};

export type ContainerResourceUsage = {
  containerId: string;
  name: string;
  cpuPercent: string;
  memoryUsage: string;
  memoryPercent: string;
  networkIo: string;
  blockIo: string;
  pids: string;
};

export type SystemResourceSnapshot = {
  status: "available" | "unavailable";
  errorMessage?: string | null;
  cpuLimit: string;
  memoryLimit: string;
  pidsLimit: string;
  networkDisabled: boolean;
  containers: ContainerResourceUsage[];
};
