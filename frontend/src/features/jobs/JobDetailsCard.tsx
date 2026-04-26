import { JobStatusBadge } from "./JobStatusBadge";
import type { JobDetails } from "./types";

export function JobDetailsCard({ job }: { job: JobDetails }) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Repository</p>
        <p className="mt-2 break-all text-sm font-medium text-ink">{job.repositoryUrl}</p>
      </div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Status</p>
        <div className="mt-2">
          <JobStatusBadge status={job.status} />
        </div>
      </div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Detected stack</p>
        <p className="mt-2 text-sm font-medium text-ink">{job.detectedStack || "pending"}</p>
      </div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Image tag</p>
        <p className="mt-2 break-all font-mono text-sm text-ink">{job.generatedImageTag || "not built yet"}</p>
      </div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Created</p>
        <p className="mt-2 text-sm text-ink">{new Date(job.createdAtUtc).toLocaleString()}</p>
      </div>
      <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
        <p className="text-xs uppercase tracking-[0.2em] text-steel">Branch</p>
        <p className="mt-2 text-sm text-ink">{job.branch || "default branch"}</p>
      </div>
      {job.errorMessage ? (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 md:col-span-2">
          <p className="text-xs uppercase tracking-[0.2em] text-rose-700">Error</p>
          <p className="mt-2 text-sm text-rose-800">{job.errorMessage}</p>
        </div>
      ) : null}
    </div>
  );
}
