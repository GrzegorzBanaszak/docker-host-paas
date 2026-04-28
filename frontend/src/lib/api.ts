import type {
  CreateJobInput,
  JobDetails,
  JobFile,
  JobFileContent,
  JobListItem,
  JobLog
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
  getRepositoryBranches: (repositoryUrl: string) =>
    request<string[]>(`/api/jobs/branches?repositoryUrl=${encodeURIComponent(repositoryUrl)}`),
  getJobs: () => request<JobListItem[]>("/api/jobs"),
  getJob: (jobId: string) => request<JobDetails>(`/api/jobs/${jobId}`),
  getLogs: (jobId: string) => request<JobLog | null>(`/api/jobs/${jobId}/logs`, { allowNotFound: true }),
  getFiles: (jobId: string) => request<JobFile[]>(`/api/jobs/${jobId}/files`),
  getFileContent: (jobId: string, fileId: string) =>
    request<JobFileContent | null>(`/api/jobs/${jobId}/files/${fileId}`, { allowNotFound: true }),
  retryJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/retry`, { method: "POST" }),
  cancelJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/cancel`, { method: "POST" })
};
