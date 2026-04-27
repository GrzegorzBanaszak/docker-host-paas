import type { JobDetails } from "./types";
import { JobStatusBadge } from "./JobStatusBadge";

export function JobDetailsCard({ job }: { job: JobDetails }) {
  return (
    <div className="grid gap-3">
      <div className="rounded border border-outline bg-surface p-4">
        <h3 className="mb-3 border-b border-outline pb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
          Artifacts
        </h3>
        <div className="space-y-4">
          <div>
            <p className="text-xs text-steel">Detected Stack</p>
            <p className="mt-1 font-mono text-[13px] text-ink">{job.detectedStack || "pending"}</p>
          </div>
          <div>
            <p className="text-xs text-steel">Image Tag</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink">
              {job.generatedImageTag || "-"}
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Deployment URL</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink">
              {job.deploymentUrl ? (
                <a className="text-secondary underline-offset-2 hover:underline" href={job.deploymentUrl} target="_blank" rel="noreferrer">
                  {job.deploymentUrl}
                </a>
              ) : (
                "-"
              )}
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Status</p>
            <div className="mt-2">
              <JobStatusBadge status={job.status} />
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Repository</p>
            <p className="mt-1 break-all text-sm text-ink">{job.repositoryUrl}</p>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Branch</p>
              <p className="mt-1 font-mono text-[12px] text-ink">{job.branch || "main"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Created</p>
              <p className="mt-1 text-sm text-ink">{new Date(job.createdAtUtc).toLocaleString()}</p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Container</p>
              <p className="mt-1 break-all font-mono text-[12px] text-ink">{job.containerName || "-"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Ports</p>
              <p className="mt-1 font-mono text-[12px] text-ink">
                {job.publishedPort && job.containerPort ? `${job.publishedPort} -> ${job.containerPort}` : "-"}
              </p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Started</p>
              <p className="mt-1 text-sm text-ink">{job.startedAtUtc ? new Date(job.startedAtUtc).toLocaleString() : "-"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Deployed</p>
              <p className="mt-1 text-sm text-ink">{job.deployedAtUtc ? new Date(job.deployedAtUtc).toLocaleString() : "-"}</p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Completed</p>
              <p className="mt-1 text-sm text-ink">{job.completedAtUtc ? new Date(job.completedAtUtc).toLocaleString() : "-"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Container ID</p>
              <p className="mt-1 font-mono text-[12px] text-ink">{job.containerId || "-"}</p>
            </div>
          </div>
        </div>
      </div>
      {job.errorMessage ? (
        <div className="rounded border border-rose-300 bg-rose-50 p-4">
          <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-rose-700">Error</p>
          <p className="mt-2 text-sm text-rose-800">{job.errorMessage}</p>
        </div>
      ) : null}
    </div>
  );
}
