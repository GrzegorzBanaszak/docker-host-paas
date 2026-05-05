import type { JobDetails } from "./types";
import { StackBadge } from "../../components/StackBadge";

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
              <p className="text-xs text-steel">Project Path</p>
              <p className="mt-1 font-mono text-[12px] text-ink">{job.projectPath || "/"}</p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Created</p>
              <p className="mt-1 text-sm text-ink">{new Date(job.createdAtUtc).toLocaleString()}</p>
            </div>
            <div />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Started</p>
              <p className="mt-1 text-sm text-ink">{job.startedAtUtc ? new Date(job.startedAtUtc).toLocaleString() : "-"}</p>
            </div>
            <div />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Completed</p>
              <p className="mt-1 text-sm text-ink">{job.completedAtUtc ? new Date(job.completedAtUtc).toLocaleString() : "-"}</p>
            </div>
            <div />
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
