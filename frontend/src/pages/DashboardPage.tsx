import { Panel } from "../components/Panel";
import { PageHeader } from "../components/PageHeader";
import { JobCreateForm } from "../features/jobs/JobCreateForm";
import { JobList } from "../features/jobs/JobList";
import { useJobs } from "../features/jobs/hooks";

export function DashboardPage() {
  const jobsQuery = useJobs();
  const jobs = jobsQuery.data ?? [];
  const successCount = jobs.filter((job) => job.status === "Succeeded").length;
  const failedCount = jobs.filter((job) => job.status === "Failed").length;

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Frontend MVP"
        title="Containerization control room"
        description="Submit GitHub repositories, watch worker progress, inspect generated container artifacts and verify Docker build results."
      />

      <div className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
        <Panel title="Create a job" description="Start a new backend workflow with repository URL and optional branch.">
          <JobCreateForm />
        </Panel>

        <Panel title="Live counters" description="Quick visibility into the current state of recent processing.">
          <div className="grid gap-4 sm:grid-cols-3">
            <StatCard label="Total" value={jobs.length} />
            <StatCard label="Succeeded" value={successCount} accent="text-mint" />
            <StatCard label="Failed" value={failedCount} accent="text-rose" />
          </div>
        </Panel>
      </div>

      <Panel title="Recent jobs" description="Jobs refresh automatically every few seconds.">
        <JobList jobs={jobs.slice(0, 8)} />
      </Panel>
    </div>
  );
}

function StatCard({ label, value, accent }: { label: string; value: number; accent?: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5">
      <p className="text-xs uppercase tracking-[0.2em] text-steel">{label}</p>
      <p className={`mt-2 text-3xl font-semibold text-ink ${accent ?? ""}`}>{value}</p>
    </div>
  );
}
