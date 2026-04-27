import type { JobStatus } from "./types";

const statusStyles: Record<JobStatus, string> = {
  Queued: "border-slate-300 bg-slate-100 text-slate-700 before:bg-slate-400",
  Running: "border-emerald-300 bg-emerald-100 text-emerald-800 before:bg-emerald-500",
  Succeeded: "border-emerald-200 bg-white text-emerald-800 before:bg-emerald-600",
  Failed: "border-rose-300 bg-rose-100 text-rose-800 before:bg-rose-600",
  Canceled: "border-amber-300 bg-amber-100 text-amber-800 before:bg-amber-500"
};

export function JobStatusBadge({ status }: { status: JobStatus }) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-sm border px-2 py-0.5 text-[11px] font-semibold uppercase tracking-[0.08em] before:h-1.5 before:w-1.5 before:rounded-full before:content-[''] ${statusStyles[status]}`}
    >
      {status}
    </span>
  );
}
