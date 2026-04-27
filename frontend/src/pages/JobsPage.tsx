import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { JobList } from "../features/jobs/JobList";
import { useJobs } from "../features/jobs/hooks";

export function JobsPage() {
  const jobsQuery = useJobs();

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Operations"
        title="Job Management"
        description="Monitor and manage container build and deployment pipelines."
      />

      <div className="rounded border border-outline bg-white p-3">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div className="flex flex-wrap items-center gap-2">
            <span className="mr-2 text-sm font-medium text-steel">Status:</span>
            {["All", "Queued", "Running", "Succeeded", "Failed", "Canceled"].map((label, index) => (
              <button
                key={label}
                className={`rounded border px-3 py-1 text-sm transition ${index === 0 ? "border-outline bg-variant text-ink" : "border-outline bg-white text-steel hover:bg-slate-50"}`}
                type="button"
              >
                {label}
              </button>
            ))}
          </div>
          <div className="relative w-full md:w-64">
            <span className="material-symbols-outlined absolute left-2 top-1/2 -translate-y-1/2 text-[16px] text-slate-400">
              search
            </span>
            <input
              className="h-9 w-full rounded border border-outline bg-white pl-8 pr-3 text-sm outline-none focus:border-sky"
              placeholder="Filter by Repository URL..."
              type="text"
            />
          </div>
        </div>
      </div>

      <Panel>
        <JobList jobs={jobsQuery.data ?? []} mode="full" />
      </Panel>
    </div>
  );
}
