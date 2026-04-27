import { Link } from "react-router-dom";
import { JobStatusBadge } from "./JobStatusBadge";
import type { JobListItem } from "./types";
import { EmptyState } from "../../components/EmptyState";

export function JobList({ jobs, mode = "full" }: { jobs: JobListItem[]; mode?: "dashboard" | "full" }) {
  if (jobs.length === 0) {
    return <EmptyState title="No jobs yet" description="Create the first containerization job from the dashboard." />;
  }

  return (
    <div className="overflow-hidden rounded border border-outline">
      <table className="min-w-full border-collapse bg-white">
        <thead>
          <tr className="border-b border-outline bg-slate-50 text-left text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
            {mode === "full" ? <th className="px-4 py-3">Job ID</th> : null}
            <th className="px-4 py-3">Repository</th>
            <th className="px-4 py-3">Branch</th>
            <th className="px-4 py-3">Stack</th>
            <th className="px-4 py-3">Status</th>
            {mode === "full" ? <th className="px-4 py-3">Image Tag</th> : null}
            <th className="px-4 py-3">Created</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody className="text-sm">
          {jobs.map((job) => (
            <tr key={job.id} className="border-b border-outline last:border-b-0 hover:bg-[rgba(211,228,254,0.3)]">
              {mode === "full" ? (
                <td className="px-4 py-3 font-mono text-[12px] text-ink">#{job.id.slice(0, 8)}</td>
              ) : null}
              <td className="px-4 py-3 font-medium text-secondary">{job.repositoryUrl}</td>
              <td className="px-4 py-3">
                <span className="font-mono text-[12px] text-steel">{job.branch || "main"}</span>
              </td>
              <td className="px-4 py-3">
                <span className="rounded border border-outline bg-surface-low px-2 py-1 text-[11px] font-medium text-steel">
                  {job.detectedStack || "pending"}
                </span>
              </td>
              <td className="px-4 py-3">
                <JobStatusBadge status={job.status} />
              </td>
              {mode === "full" ? (
                <td className="px-4 py-3 font-mono text-[11px] text-steel">{job.generatedImageTag || "-"}</td>
              ) : null}
              <td className="px-4 py-3 text-steel">{formatAge(job.createdAtUtc)}</td>
              <td className="px-4 py-3 text-right">
                <Link className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink" to={`/jobs/${job.id}`}>
                  Details
                  <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
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
