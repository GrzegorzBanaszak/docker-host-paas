import { Link } from "react-router-dom";
import type { JobImageSummary } from "../jobs/types";
import { EmptyState } from "../../components/EmptyState";
import { JobStatusBadge } from "../jobs/JobStatusBadge";
import { StackBadge } from "../../components/StackBadge";

type JobImageListProps = {
  images: JobImageSummary[];
  selectedImageId?: string;
  onSelect: (imageId: string) => void;
};

export function JobImageList({ images, selectedImageId, onSelect }: JobImageListProps) {
  if (images.length === 0) {
    return <EmptyState title="No images yet" description="Run the job to create the first Docker image snapshot." />;
  }

  return (
    <div className="space-y-2">
      {images.map((image) => {
        const isActive = selectedImageId === image.id;

        return (
          <button
            key={image.id}
            type="button"
            onClick={() => onSelect(image.id)}
            className={`w-full rounded border p-3 text-left transition ${
              isActive ? "border-slate-900 bg-slate-50" : "border-outline bg-white hover:bg-slate-50"
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-mono text-[11px] text-ink">#{image.id.slice(0, 8)}</span>
                  {image.isCurrent ? (
                    <span className="rounded border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.08em] text-emerald-700">
                      Current
                    </span>
                  ) : null}
                </div>
                <p className="mt-1 truncate font-mono text-[11px] text-steel" title={image.imageTag || "-"}>
                  {image.imageTag || "build pending"}
                </p>
                <div className="mt-2 flex flex-wrap items-center gap-2">
                  <StackBadge stack={image.detectedStack} compact />
                  <span className="text-[12px] text-steel">{formatAge(image.createdAtUtc)}</span>
                </div>
              </div>
              <JobStatusBadge status={image.status} />
            </div>
            <div className="mt-3 flex items-center justify-between gap-3">
              <span className="font-mono text-[11px] text-steel">
                {image.sourceCommitSha ? image.sourceCommitSha.slice(0, 12) : "commit pending"}
              </span>
              <Link
                to={`/images/${image.id}`}
                onClick={(event) => event.stopPropagation()}
                className="inline-flex items-center gap-1 text-[11px] font-bold uppercase tracking-[0.08em] text-secondary hover:text-ink"
              >
                Details
                <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
              </Link>
            </div>
          </button>
        );
      })}
    </div>
  );
}

function formatAge(value: string) {
  const createdAt = new Date(value).getTime();
  const diffMinutes = Math.max(1, Math.round((Date.now() - createdAt) / 60000));

  if (diffMinutes < 60) {
    return `${diffMinutes} min ago`;
  }

  const diffHours = Math.round(diffMinutes / 60);
  if (diffHours < 24) {
    return `${diffHours} h ago`;
  }

  const diffDays = Math.round(diffHours / 24);
  return `${diffDays} d ago`;
}
