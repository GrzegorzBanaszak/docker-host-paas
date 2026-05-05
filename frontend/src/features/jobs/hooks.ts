import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../lib/api";
import type { CreateJobInput, CreateProjectInput, CreateProjectJobInput } from "./types";

export function useJobs() {
  return useQuery({
    queryKey: ["jobs"],
    queryFn: api.getJobs,
    refetchInterval: 5000
  });
}

export function useProjects() {
  return useQuery({
    queryKey: ["projects"],
    queryFn: api.getProjects,
    refetchInterval: 5000
  });
}

export function useProject(projectId: string) {
  return useQuery({
    queryKey: ["project", projectId],
    queryFn: () => api.getProject(projectId),
    enabled: Boolean(projectId),
    refetchInterval: 5000
  });
}

export function useSystemResources() {
  return useQuery({
    queryKey: ["system", "resources"],
    queryFn: api.getSystemResources,
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
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
    }
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateProjectInput) => api.createProject(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
    }
  });
}

export function useCreateProjectJob(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload?: CreateProjectJobInput) => api.createProjectJob(projectId, payload ?? {}),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
      void queryClient.invalidateQueries({ queryKey: ["project", projectId] });
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
    }
  });
}

export function usePublishProject(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.publishProject(projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
      void queryClient.invalidateQueries({ queryKey: ["project", projectId] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
    }
  });
}

export function useUnpublishProject(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.unpublishProject(projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
      void queryClient.invalidateQueries({ queryKey: ["project", projectId] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
    }
  });
}

export function useArchiveProject(projectId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.archiveProject(projectId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
      void queryClient.invalidateQueries({ queryKey: ["project", projectId] });
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
    }
  });
}

export function useRepositoryBranches() {
  return useMutation({
    mutationFn: ({ repositoryUrl, projectPath }: { repositoryUrl: string; projectPath?: string }) =>
      api.getRepositoryInspection(repositoryUrl, projectPath)
  });
}

export function useRetryJob(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.retryJob(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
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
      void queryClient.invalidateQueries({ queryKey: ["projects"] });
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
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
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
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
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
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
    }
  });
}

export function usePublishDnsRoute(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.publishDnsRoute(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
    }
  });
}

export function useUnpublishDnsRoute(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.unpublishDnsRoute(jobId),
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

export function useDeleteJob(jobId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => api.deleteJob(jobId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["jobs"] });
      void queryClient.invalidateQueries({ queryKey: ["images"] });
      void queryClient.invalidateQueries({ queryKey: ["dns"] });
      void queryClient.invalidateQueries({ queryKey: ["job", jobId] });
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
