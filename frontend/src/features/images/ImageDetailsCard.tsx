import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { ImageDetails } from "../jobs/types";
import { JobStatusBadge } from "../jobs/JobStatusBadge";
import { useDeleteImage } from "../jobs/hooks";
import { StackBadge } from "../../components/StackBadge";

export function ImageDetailsCard({ image }: { image: ImageDetails }) {
  const [copied, setCopied] = useState(false);
  const navigate = useNavigate();
  const deleteImage = useDeleteImage(image.id);

  async function handleCopy() {
    if (!image.imageId || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(image.imageId);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <div className="grid gap-3">
      <div className="rounded border border-outline bg-surface p-4">
        <div className="mb-3 flex items-start justify-between gap-3 border-b border-outline pb-2">
          <div>
            <h3 className="text-[11px] font-bold uppercase tracking-[0.12em] text-steel">Image Snapshot</h3>
            <p className="mt-1 font-mono text-[11px] text-ink">#{image.id.slice(0, 8)}</p>
          </div>
          <div className="flex items-center gap-2">
            {image.isCurrent ? (
              <span className="rounded border border-emerald-200 bg-emerald-50 px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] text-emerald-700">
                Current
              </span>
            ) : null}
            <button
              type="button"
              onClick={() =>
                deleteImage.mutate(undefined, {
                  onSuccess: () => navigate("/images")
                })
              }
              disabled={deleteImage.isPending || image.isCurrent}
              className="inline-flex items-center gap-1 rounded border border-outline bg-white px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] text-steel transition hover:bg-slate-50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-60"
            >
              <span className="material-symbols-outlined text-[14px]">delete</span>
              Delete
            </button>
          </div>
        </div>
        <div className="space-y-4">
          <div>
            <p className="text-xs text-steel">Status</p>
            <div className="mt-2">
              <JobStatusBadge status={image.status} />
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Image Tag</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink">
              {image.imageTag || "-"}
            </div>
          </div>
          <div>
            <div className="flex items-center justify-between gap-3">
              <p className="text-xs text-steel">Image ID</p>
              {image.imageId ? (
                <button
                  type="button"
                  onClick={() => void handleCopy()}
                  className="inline-flex items-center gap-1 rounded-sm border border-outline bg-white px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] text-steel transition hover:bg-slate-50 hover:text-ink"
                >
                  <span className="material-symbols-outlined text-[14px]">{copied ? "check" : "content_copy"}</span>
                  {copied ? "Copied" : "Copy"}
                </button>
              ) : null}
            </div>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink" title={image.imageId || "-"}>
              {image.imageId ? abbreviateMiddle(image.imageId, 18, 12) : "-"}
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Detected Stack</p>
              <div className="mt-2">
                <StackBadge stack={image.detectedStack} />
              </div>
            </div>
            <div>
              <p className="text-xs text-steel">Container Port</p>
              <p className="mt-1 font-mono text-[12px] text-ink">{image.containerPort || "-"}</p>
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Commit</p>
            <div className="mt-1 rounded border border-outline bg-variant px-2 py-1 font-mono text-[11px] text-ink">
              {image.sourceCommitSha || "-"}
            </div>
          </div>
          <div>
            <p className="text-xs text-steel">Job</p>
            <p className="mt-1 text-sm font-semibold text-ink">{image.jobName}</p>
            <p className="mt-1 break-all text-sm text-steel">{image.repositoryUrl}</p>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Branch</p>
              <p className="mt-1 font-mono text-[12px] text-ink">{image.branch || "main"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Job Status</p>
              <p className="mt-1 text-sm text-ink">{image.jobStatus}</p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Created</p>
              <p className="mt-1 text-sm text-ink">{new Date(image.createdAtUtc).toLocaleString()}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Started</p>
              <p className="mt-1 text-sm text-ink">{image.startedAtUtc ? new Date(image.startedAtUtc).toLocaleString() : "-"}</p>
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div>
              <p className="text-xs text-steel">Built</p>
              <p className="mt-1 text-sm text-ink">{image.builtAtUtc ? new Date(image.builtAtUtc).toLocaleString() : "-"}</p>
            </div>
            <div>
              <p className="text-xs text-steel">Completed</p>
              <p className="mt-1 text-sm text-ink">{image.completedAtUtc ? new Date(image.completedAtUtc).toLocaleString() : "-"}</p>
            </div>
          </div>
        </div>
      </div>
      {image.errorMessage ? (
        <div className="rounded border border-rose-300 bg-rose-50 p-4">
          <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-rose-700">Error</p>
          <p className="mt-2 text-sm text-rose-800">{image.errorMessage}</p>
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
