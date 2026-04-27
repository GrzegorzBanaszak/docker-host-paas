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
    const message = await response.text();
    throw new Error(message || "Request failed.");
  }

  return response.json() as Promise<T>;
}

export const api = {
  createJob: (payload: CreateJobInput) =>
    request<JobDetails>("/api/jobs", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
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
