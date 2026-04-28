import type { JobDetails } from "./types";
import { useCancelJob, useRebuildJob, useRestartContainer, useStartContainer, useStopContainer } from "./hooks";

export function JobActions({ job }: { job: JobDetails }) {
  const rebuildJob = useRebuildJob(job.id);
  const cancelJob = useCancelJob(job.id);
  const startContainer = useStartContainer(job.id);
  const restartContainer = useRestartContainer(job.id);
  const stopContainer = useStopContainer(job.id);
  const hasRunnableImage = Boolean(job.currentImage?.imageTag || job.generatedImageTag);
  const isContainerRunning = job.containerStatus === "running";
  const canMutateContainer = hasRunnableImage && job.status !== "Running";
  const canOpenDeployment = Boolean(job.deploymentUrl && isContainerRunning);

  return (
    <div className="grid gap-2 sm:min-w-[20rem]">
      <div className="grid grid-cols-3 gap-2">
        <button
          type="button"
          onClick={() => startContainer.mutate()}
          disabled={startContainer.isPending || !canMutateContainer || isContainerRunning}
          className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">play_arrow</span>
          Start
        </button>
        <button
          type="button"
          onClick={() => stopContainer.mutate()}
          disabled={stopContainer.isPending || !canMutateContainer || !["running", "restarting"].includes(job.containerStatus ?? "")}
          className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">stop</span>
          Stop
        </button>
        <button
          type="button"
          onClick={() => restartContainer.mutate()}
          disabled={restartContainer.isPending || !canMutateContainer}
          className="flex items-center justify-center gap-1 rounded border border-outline bg-white px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">restart_alt</span>
          Restart
        </button>
      </div>
      <div className="grid grid-cols-3 gap-2">
        {canOpenDeployment ? (
          <a
            href={job.deploymentUrl ?? undefined}
            target="_blank"
            rel="noreferrer"
            className="flex items-center justify-center gap-1 rounded border border-secondary bg-white px-3 py-1.5 text-sm font-medium text-secondary transition hover:bg-[rgba(211,228,254,0.35)]"
          >
            <span className="material-symbols-outlined text-[16px]">open_in_new</span>
            Open
          </a>
        ) : (
          <span className="flex items-center justify-center gap-1 rounded border border-outline bg-surface px-3 py-1.5 text-sm font-medium text-steel opacity-70">
            <span className="material-symbols-outlined text-[16px]">open_in_new</span>
            Open
          </span>
        )}
        <button
          type="button"
          onClick={() => rebuildJob.mutate()}
          disabled={rebuildJob.isPending || job.status === "Running"}
          className="flex items-center justify-center gap-1 rounded border border-slate-900 bg-slate-900 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">refresh</span>
          Rebuild
        </button>
        <button
          type="button"
          onClick={() => cancelJob.mutate()}
          disabled={cancelJob.isPending || ["Succeeded", "Failed", "Canceled"].includes(job.status)}
          className="flex items-center justify-center gap-1 rounded border border-outline bg-surface px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-variant disabled:cursor-not-allowed disabled:opacity-60"
        >
          <span className="material-symbols-outlined text-[16px]">cancel</span>
          Cancel
        </button>
      </div>
    </div>
  );
}
