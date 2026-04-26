import type { JobDetails } from "./types";
import { useCancelJob, useRetryJob } from "./hooks";

export function JobActions({ job }: { job: JobDetails }) {
  const retryJob = useRetryJob(job.id);
  const cancelJob = useCancelJob(job.id);

  return (
    <div className="flex flex-wrap gap-3">
      <button
        type="button"
        onClick={() => retryJob.mutate()}
        disabled={retryJob.isPending || job.status === "Running"}
        className="rounded-full border border-slate-300 px-4 py-2 text-sm font-semibold text-ink transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
      >
        Retry
      </button>
      <button
        type="button"
        onClick={() => cancelJob.mutate()}
        disabled={cancelJob.isPending || ["Succeeded", "Failed", "Canceled"].includes(job.status)}
        className="rounded-full border border-rose-300 px-4 py-2 text-sm font-semibold text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60"
      >
        Cancel
      </button>
    </div>
  );
}
