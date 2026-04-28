import { Link } from "react-router-dom";
import { EmptyState } from "../../components/EmptyState";
import { JobStatusBadge } from "../jobs/JobStatusBadge";
import type { ImageListItem } from "../jobs/types";
import { GitHubRepoLink } from "../../components/GitHubRepoLink";
import { StackBadge } from "../../components/StackBadge";

export function ImageCatalog({ images }: { images: ImageListItem[] }) {
  if (images.length === 0) {
    return <EmptyState title="No images yet" description="Built images will appear here after the first successful pipeline run." />;
  }

  return (
    <div className="overflow-hidden rounded border border-outline">
      <table className="min-w-full border-collapse bg-white">
        <thead>
          <tr className="border-b border-outline bg-slate-50 text-left text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
            <th className="px-4 py-3">Image</th>
            <th className="px-4 py-3">Job</th>
            <th className="px-4 py-3">Stack</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Tag</th>
            <th className="px-4 py-3">Commit</th>
            <th className="px-4 py-3">Created</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody className="text-sm">
          {images.map((image) => (
            <tr key={image.id} className="border-b border-outline last:border-b-0 hover:bg-[rgba(211,228,254,0.3)]">
              <td className="px-4 py-3">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="font-mono text-[12px] text-ink">#{image.id.slice(0, 8)}</span>
                    {image.isCurrent ? (
                      <span className="rounded border border-emerald-200 bg-emerald-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.08em] text-emerald-700">
                        Current
                      </span>
                    ) : null}
                  </div>
                  <div className="font-mono text-[11px] text-steel">{image.imageId ? abbreviateMiddle(image.imageId, 12, 10) : "-"}</div>
                </div>
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center gap-2">
                  <div className="min-w-0 font-medium text-secondary">{image.jobName}</div>
                  <GitHubRepoLink href={image.repositoryUrl} />
                </div>
              </td>
              <td className="px-4 py-3">
                <StackBadge stack={image.detectedStack} compact />
              </td>
              <td className="px-4 py-3">
                <JobStatusBadge status={image.status} />
              </td>
              <td className="px-4 py-3 font-mono text-[11px] text-steel">{image.imageTag || "-"}</td>
              <td className="px-4 py-3 font-mono text-[11px] text-steel">
                {image.sourceCommitSha ? image.sourceCommitSha.slice(0, 12) : "-"}
              </td>
              <td className="px-4 py-3 text-steel">{formatAge(image.createdAtUtc)}</td>
              <td className="px-4 py-3 text-right">
                <div className="flex items-center justify-end gap-3">
                  <Link className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink" to={`/jobs/${image.jobId}`}>
                    Job
                    <span className="material-symbols-outlined text-[14px]">arrow_outward</span>
                  </Link>
                  <Link className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink" to={`/images/${image.id}`}>
                    Details
                    <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
                  </Link>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function abbreviateMiddle(value: string, startLength: number, endLength: number) {
  if (value.length <= startLength + endLength + 3) {
    return value;
  }

  return `${value.slice(0, startLength)}...${value.slice(-endLength)}`;
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
