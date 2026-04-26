import type {
  CreateJobInput,
  JobDetails,
  JobFile,
  JobFileContent,
  JobListItem,
  JobLog
} from "../features/jobs/types";

async function request<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    headers: {
      "Content-Type": "application/json"
    },
    ...init
  });

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
  getLogs: (jobId: string) => request<JobLog>(`/api/jobs/${jobId}/logs`),
  getFiles: (jobId: string) => request<JobFile[]>(`/api/jobs/${jobId}/files`),
  getFileContent: (jobId: string, fileId: string) =>
    request<JobFileContent>(`/api/jobs/${jobId}/files/${fileId}`),
  retryJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/retry`, { method: "POST" }),
  cancelJob: (jobId: string) =>
    request<JobDetails>(`/api/jobs/${jobId}/cancel`, { method: "POST" })
};
