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

export function useImages() {
  return useQuery({
    queryKey: ["images"],
    queryFn: api.getImages,
    refetchInterval: 5000
  });
}

export function useImage(imageId: string) {
  return useQuery({
    queryKey: ["image", imageId],
    queryFn: () => api.getImage(imageId),
    enabled: Boolean(imageId),
    refetchInterval: 5000
  });
}

export function useImageLogs(imageId: string) {
  return useQuery({
    queryKey: ["image", imageId, "logs"],
    queryFn: () => api.getImageLogs(imageId),
    enabled: Boolean(imageId),
    refetchInterval: 5000
  });
}

export function useImageFiles(imageId: string) {
  return useQuery({
    queryKey: ["image", imageId, "files"],
    queryFn: () => api.getImageFiles(imageId),
    enabled: Boolean(imageId),
    refetchInterval: 5000
  });
}

export function useImageFileContent(imageId: string, fileId?: string) {
  return useQuery({
    queryKey: ["image", imageId, "files", fileId],
    queryFn: () => api.getImageFileContent(imageId, fileId!),
    enabled: Boolean(imageId && fileId)
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

export function useRepositoryBranches() {
  return useMutation({
    mutationFn: (repositoryUrl: string) => api.getRepositoryInspection(repositoryUrl)
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

export function useRebuildJob(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.rebuildJob(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
      void queryClient.invalidateQueries({ queryKey: ["images"] });
    }
  });
}

export function useStartContainer(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.startContainer(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
    }
  });
}

export function useRestartContainer(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.restartContainer(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
    }
  });
}

export function useStopContainer(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.stopContainer(jobId),
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
      void queryClient.invalidateQueries({ queryKey: ["images"] });
    }
  });
}

export function useDeleteImage(imageId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.deleteImage(imageId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["images"] });
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job"] });
      void queryClient.invalidateQueries({ queryKey: ["image", imageId] });
    }
  });
}
