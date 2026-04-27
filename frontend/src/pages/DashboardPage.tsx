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
  const runningCount = jobs.filter((job) => job.status === "Running").length;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Control Room"
        title="DOCKERIZER"
        description="Containerize your GitHub repositories with precision."
      />

      <section className="grid gap-4 xl:grid-cols-4">
        <StatCard label="Total Jobs" value={jobs.length} icon="query_stats" />
        <StatCard label="Succeeded" value={successCount} icon="check_circle" iconTone="text-emerald-600" />
        <StatCard label="Failed" value={failedCount} icon="error" iconTone="text-rose-700" />
        <StatCard label="Running" value={runningCount} icon="autorenew" iconTone="text-secondary" />
      </section>

      <div className="grid grid-cols-12 gap-4">
        <div className="col-span-12 xl:col-span-4">
          <Panel title="Job Creation">
            <JobCreateForm />
          </Panel>
        </div>

        <div className="col-span-12 xl:col-span-8">
          <Panel title="Recent Jobs" description="Jobs refresh automatically every few seconds.">
            <JobList jobs={jobs.slice(0, 8)} mode="dashboard" />
          </Panel>
        </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-4">
        <Panel title="System Status">
          <div className="flex items-center gap-2 text-sm text-steel">
            <span className="h-2 w-2 rounded-full bg-mint" />
            Engine Capacity: 84% Available
          </div>
        </Panel>
        <div className="xl:col-span-3" />
      </div>
    </div>
  );
}

function StatCard({ label, value, icon, iconTone }: { label: string; value: number; icon: string; iconTone?: string }) {
  return (
    <div className="relative overflow-hidden rounded border border-outline bg-white p-4">
      <div className="absolute right-0 top-0 h-16 w-16 bg-gradient-to-bl from-variant to-transparent opacity-50" />
      <div className="relative z-10 flex items-start justify-between">
        <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
        <span className={`material-symbols-outlined text-[18px] ${iconTone ?? "text-slate-400"}`}>{icon}</span>
      </div>
      <p className="relative z-10 mt-3 text-[32px] font-bold tracking-[-0.02em] text-ink">{value}</p>
    </div>
  );
}
