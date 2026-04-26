import { Link } from "react-router-dom";
import { JobStatusBadge } from "./JobStatusBadge";
import type { JobListItem } from "./types";
import { EmptyState } from "../../components/EmptyState";

export function JobList({ jobs }: { jobs: JobListItem[] }) {
  if (jobs.length === 0) {
    return <EmptyState title="No jobs yet" description="Create the first containerization job from the dashboard." />;
  }

  return (
    <div className="overflow-hidden rounded-3xl border border-slate-200">
      <table className="min-w-full divide-y divide-slate-200 bg-white">
        <thead className="bg-slate-50">
          <tr className="text-left text-xs uppercase tracking-[0.2em] text-steel">
            <th className="px-4 py-3">Repository</th>
            <th className="px-4 py-3">Stack</th>
            <th className="px-4 py-3">Status</th>
            <th className="px-4 py-3">Created</th>
            <th className="px-4 py-3"></th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 text-sm">
          {jobs.map((job) => (
            <tr key={job.id}>
              <td className="px-4 py-4">
                <div className="font-medium text-ink">{job.repositoryUrl}</div>
                <div className="text-xs text-steel">{job.branch || "default branch"}</div>
              </td>
              <td className="px-4 py-4 text-steel">{job.detectedStack || "pending"}</td>
              <td className="px-4 py-4">
                <JobStatusBadge status={job.status} />
              </td>
              <td className="px-4 py-4 text-steel">{new Date(job.createdAtUtc).toLocaleString()}</td>
              <td className="px-4 py-4 text-right">
                <Link className="font-semibold text-sky hover:text-sky-700" to={`/jobs/${job.id}`}>
                  Details
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
