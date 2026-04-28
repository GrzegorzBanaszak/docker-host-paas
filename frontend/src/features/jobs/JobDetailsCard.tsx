import { useState } from "react";
import type { JobDetails } from "./types";
import { JobStatusBadge } from "./JobStatusBadge";
import { StackBadge } from "../../components/StackBadge";

export function JobDetailsCard({ job }: { job: JobDetails }) {
  const [copied, setCopied] = useState(false);

  async function handleCopyContainerId() {
    if (!job.containerId || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(job.containerId);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <div className="grid gap-3">
      <div className="rounded border border-outline bg-surface p-4">
        <h3 className="mb-3 border-b border-outline pb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
          Artifacts
        </h3>
        <div className="space-y-4">
          <div>
            <p className="text-xs text-steel">Job Name</p>
            <p className="mt-1 text-sm font-semibold text-ink">{job.name}</p>
          </div>
          <div>
            <p className="text-xs text-steel">Detected Stack</p>
            <div className="mt-2">
              <StackBadge stack={job.detectedStack} />
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Image Tag</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink">
              {job.currentImage?.imageTag || job.generatedImageTag || "-"}
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Image ID</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink" title={job.currentImage?.imageId || job.imageId || "-"}>
              {job.currentImage?.imageId || job.imageId ? abbreviateMiddle(job.currentImage?.imageId || job.imageId || "", 18, 12) : "-"}
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Current Image</p>
              <p className="mt-1 font-mono text-[12px] text-ink">
                {job.currentImage ? `#${job.currentImage.id.slice(0, 8)}` : "not assigned"}
              </p>
            </div>
            <div>
              <p className="text-xs text-steel">Image History</p>
              <p className="mt-1 text-sm text-ink">{job.images.length}</p>
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
            <p className="text-xs text-steel">Container Status</p>
            <div className="mt-2">
              <ContainerStatusBadge status={job.containerStatus} />
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
            <div />
          </div>
          <div>
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs text-steel">Container ID</p>
              {job.containerId ? (
                <button
                  type="button"
                  onClick={() => void handleCopyContainerId()}
                  className="inline-flex items-center gap-1 rounded-sm border border-outline bg-white px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] text-steel transition hover:bg-slate-50 hover:text-ink"
                >
                  <span className="material-symbols-outlined text-[14px]">{copied ? "check" : "content_copy"}</span>
                  {copied ? "Copied" : "Copy"}
                </button>
              ) : null}
            </div>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink" title={job.containerId || "-"}>
              {job.containerId ? abbreviateMiddle(job.containerId, 14, 14) : "-"}
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

function abbreviateMiddle(value: string, startLength: number, endLength: number) {
  if (value.length <= startLength + endLength + 3) {
    return value;
  }

  return `${value.slice(0, startLength)}...${value.slice(-endLength)}`;
}

function ContainerStatusBadge({ status }: { status?: string | null }) {
  const normalized = status ?? "not_found";
  const className =
    normalized === "running"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : normalized === "restarting"
        ? "border-sky-200 bg-sky-50 text-sky-700"
        : normalized === "paused"
          ? "border-amber-200 bg-amber-50 text-amber-700"
          : normalized === "created"
            ? "border-slate-200 bg-slate-50 text-slate-700"
            : normalized === "exited" || normalized === "dead"
              ? "border-rose-200 bg-rose-50 text-rose-700"
              : "border-outline bg-surface-low text-steel";

  return (
    <span className={`inline-flex items-center rounded border px-2 py-1 text-[11px] font-bold uppercase tracking-[0.08em] ${className}`}>
      {normalized.replace("_", " ")}
    </span>
  );
}
