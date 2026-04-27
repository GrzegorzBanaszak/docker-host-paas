import type { JobDetails } from "./types";
import { useCancelJob, useRetryJob } from "./hooks";

export function JobActions({ job }: { job: JobDetails }) {
  const retryJob = useRetryJob(job.id);
  const cancelJob = useCancelJob(job.id);

  return (
    <div className="flex flex-wrap gap-3">
      {job.deploymentUrl ? (
        <a
          href={job.deploymentUrl}
          target="_blank"
          rel="noreferrer"
          className="flex items-center gap-1 rounded border border-secondary bg-white px-3 py-1.5 text-sm font-medium text-secondary transition hover:bg-[rgba(211,228,254,0.35)]"
        >
          <span className="material-symbols-outlined text-[16px]">open_in_new</span>
          Open
        </a>
      ) : null}
      <button
        type="button"
        onClick={() => retryJob.mutate()}
        disabled={retryJob.isPending || job.status === "Running"}
        className="flex items-center gap-1 rounded border border-slate-900 bg-slate-900 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
      >
        <span className="material-symbols-outlined text-[16px]">refresh</span>
        Retry
      </button>
      <button
        type="button"
        onClick={() => cancelJob.mutate()}
        disabled={cancelJob.isPending || ["Succeeded", "Failed", "Canceled"].includes(job.status)}
        className="flex items-center gap-1 rounded border border-outline bg-surface px-3 py-1.5 text-sm font-medium text-ink transition hover:bg-variant disabled:cursor-not-allowed disabled:opacity-60"
      >
        <span className="material-symbols-outlined text-[16px]">cancel</span>
        Cancel
      </button>
    </div>
  );
}
