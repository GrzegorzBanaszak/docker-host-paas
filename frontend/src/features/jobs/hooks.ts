import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../lib/api";
import type { CreateJobInput } from "./types";

export function useJobs() {
  return useQuery({
    queryKey: ["jobs"],
    queryFn: api.getJobs,
    refetchInterval: 5000
  });
}

export function useJob(jobId: string) {
  return useQuery({
    queryKey: ["job", jobId],
    queryFn: () => api.getJob(jobId),
    enabled: Boolean(jobId),
    refetchInterval: 5000
  });
}

export function useJobLogs(jobId: string) {
  return useQuery({
    queryKey: ["job", jobId, "logs"],
    queryFn: () => api.getLogs(jobId),
    enabled: Boolean(jobId),
    refetchInterval: 5000
  });
}

export function useJobFiles(jobId: string) {
  return useQuery({
    queryKey: ["job", jobId, "files"],
    queryFn: () => api.getFiles(jobId),
    enabled: Boolean(jobId),
    refetchInterval: 5000
  });
}

export function useJobFileContent(jobId: string, fileId?: string) {
  return useQuery({
    queryKey: ["job", jobId, "files", fileId],
    queryFn: () => api.getFileContent(jobId, fileId!),
    enabled: Boolean(jobId && fileId)
  });
}

export function useCreateJob() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateJobInput) => api.createJob(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
    }
  });
}

export function useRetryJob(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.retryJob(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
    }
  });
}

export function useCancelJob(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.cancelJob(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
    }
  });
}
