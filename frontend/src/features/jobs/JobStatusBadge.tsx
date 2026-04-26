import type { JobStatus } from "./types";

const statusStyles: Record<JobStatus, string> = {
  Queued: "bg-slate-100 text-slate-700",
  Running: "bg-sky-100 text-sky-700",
  Succeeded: "bg-emerald-100 text-emerald-700",
  Failed: "bg-rose-100 text-rose-700",
  Canceled: "bg-amber-100 text-amber-700"
};

export function JobStatusBadge({ status }: { status: JobStatus }) {
  return (
    <span className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${statusStyles[status]}`}>
      {status}
    </span>
  );
}
