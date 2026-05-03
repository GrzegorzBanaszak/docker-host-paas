import type {
  DnsOverview,
  DnsRoute
} from "../features/dns/types";
import type {
  CreateJobInput,
  ImageDetails,
  ImageListItem,
  JobDetails,
  JobFile,
  JobFileContent,
  JobListItem,
  JobLog,
  RepositoryInspection,
  SystemResourceSnapshot
} from "../features/jobs/types";

type RequestOptions = RequestInit & {
  allowNotFound?: boolean;
};

async function request<T>(input: RequestInfo, init?: RequestOptions): Promise<T> {
  const response = await fetch(input, {
    headers: {
      "Content-Type": "application/json"
    },
    ...init
  });

  if (response.status === 404 && init?.allowNotFound) {
    return null as T;
  }

  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }

  if (response.status === 204) {
    return null as T;
  }

  return response.json() as Promise<T>;
}

async function getErrorMessage(response: Response) {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const payload = (await response.json()) as {
      title?: string;
      detail?: string;
      errors?: Record<string, string[]>;
    };

    const firstValidationMessage = Object.values(payload.errors ?? {})
      .flat()
      .find(Boolean);

    return firstValidationMessage ?? payload.detail ?? payload.title ?? "Request failed.";
  }

  const message = await response.text();
  return message || "Request failed.";
}

export const api = {
  createJob: (payload: CreateJobInput) =>
    request<JobDetails>("/api/jobs", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  getRepositoryInspection: (repositoryUrl: string, projectPath?: string) =>
    request<RepositoryInspection>(
      `/api/jobs/branches?repositoryUrl=${encodeURIComponent(repositoryUrl)}${
        projectPath ? `&projectPath=${encodeURIComponent(projectPath)}` : ""
      }`
    ),
  getJobs: () => request<JobListItem[]>("/api/jobs"),
  getJob: (jobId: string) => request<JobDetails>(`/api/jobs/${jobId}`),
  getLogs: (jobId: string) => request<JobLog | null>(`/api/jobs/${jobId}/logs`, { allowNotFound: true }),
  getFiles: (jobId: string) => request<JobFile[]>(`/api/jobs/${jobId}/files`),
  getFileContent: (jobId: string, fileId: string) =>
    request<JobFileContent | null>(`/api/jobs/${jobId}/files/${fileId}`, { allowNotFound: true }),
  getImages: () => request<ImageListItem[]>("/api/images"),
  getDnsOverview: () => request<DnsOverview>("/api/dns/overview"),
  getDnsRoutes: () => request<DnsRoute[]>("/api/dns/routes"),
  getImage: (imageId: string) => request<ImageDetails>(`/api/images/${imageId}`),
  getImageLogs: (imageId: string) => request<JobLog | null>(`/api/images/${imageId}/logs`, { allowNotFound: true }),
  getImageFiles: (imageId: string) => request<JobFile[]>(`/api/images/${imageId}/files`),
  getImageFileContent: (imageId: string, fileId: string) =>
    request<JobFileContent | null>(`/api/images/${imageId}/files/${fileId}`, { allowNotFound: true }),
  rebuildJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/rebuild`, { method: "POST" }),
  startContainer: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/container/start`, { method: "POST" }),
  restartContainer: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/container/restart`, { method: "POST" }),
  stopContainer: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/container/stop`, { method: "POST" }),
  publishDnsRoute: (jobId: string) =>
    request<JobDetails>(`/api/dns/routes/${jobId}/publish`, { method: "POST" }),
  unpublishDnsRoute: (jobId: string) =>
    request<JobDetails>(`/api/dns/routes/${jobId}/publish`, { method: "DELETE" }),
  retryJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/retry`, { method: "POST" }),
  cancelJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/cancel`, { method: "POST" }),
  deleteImage: async (imageId: string) => {
    await request<null>(`/api/images/${imageId}`, { method: "DELETE" });
  },
  getSystemResources: () => request<SystemResourceSnapshot>("/api/system/resources")
};
