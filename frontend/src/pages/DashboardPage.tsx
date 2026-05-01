import { Link } from "react-router-dom";
import { Panel } from "../components/Panel";
import { PageHeader } from "../components/PageHeader";
import { JobList } from "../features/jobs/JobList";
import { useJobs, useSystemResources } from "../features/jobs/hooks";
import type { ContainerResourceUsage } from "../features/jobs/types";

export function DashboardPage() {
  const jobsQuery = useJobs();
  const resourcesQuery = useSystemResources();
  const jobs = jobsQuery.data ?? [];
  const resources = resourcesQuery.data;
  const successCount = jobs.filter((job) => job.status === "Succeeded").length;
  const failedCount = jobs.filter((job) => job.status === "Failed").length;
  const runningCount = jobs.filter((job) => job.status === "Running").length;
  const deployedCount = jobs.filter((job) => Boolean(job.deploymentUrl)).length;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Control Room"
        title="DOCKERIZER"
        description="Containerize your GitHub repositories with precision."
      />

      <section className="grid gap-4 xl:grid-cols-5">
        <StatCard label="Total Jobs" value={jobs.length} icon="query_stats" />
        <StatCard label="Succeeded" value={successCount} icon="check_circle" iconTone="text-emerald-600" />
        <StatCard label="Failed" value={failedCount} icon="error" iconTone="text-rose-700" />
        <StatCard label="Running" value={runningCount} icon="autorenew" iconTone="text-secondary" />
        <StatCard label="Live Deploys" value={deployedCount} icon="open_in_new" iconTone="text-sky-700" />
      </section>

      <div className="grid grid-cols-12 gap-4">
        <div className="col-span-12 xl:col-span-4">
          <Panel title="Pipeline Launch" description="Create a new containerization job from the dedicated setup view.">
            <div className="space-y-4">
              <p className="text-sm text-steel">
                Choose a repository, inspect its branches, and name the deployment before the build pipeline starts.
              </p>
              <Link
                to="/jobs/new"
                className="inline-flex h-10 items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-white transition hover:bg-slate-800"
              >
                <span className="material-symbols-outlined text-[18px]">add</span>
                New Job
              </Link>
            </div>
          </Panel>
        </div>

        <div className="col-span-12 xl:col-span-8">
          <Panel title="Recent Jobs" description="Jobs refresh automatically every few seconds.">
            <JobList jobs={jobs.slice(0, 8)} mode="dashboard" />
          </Panel>
        </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-4">
        <Panel title="Resource Guard" description="Live Docker resource usage for managed containers.">
          <div className="space-y-4">
            <div className="grid grid-cols-3 gap-2 text-xs">
              <ResourceLimit label="CPU" value={resources?.cpuLimit ?? "-"} />
              <ResourceLimit label="RAM" value={resources?.memoryLimit ?? "-"} />
              <ResourceLimit label="PIDs" value={resources?.pidsLimit ?? "-"} />
            </div>
            {resources?.status === "unavailable" ? (
              <p className="text-sm font-medium text-rose">{resources.errorMessage ?? "Docker stats unavailable."}</p>
            ) : resources && resources.containers.length > 0 ? (
              <div className="space-y-3">
                {resources.containers.map((container) => (
                  <ContainerUsageRow key={container.containerId} container={container} />
                ))}
              </div>
            ) : (
              <p className="text-sm text-steel">No managed containers are currently reporting usage.</p>
            )}
            <div className="flex items-center gap-2 text-xs text-steel">
              <span className={`h-2 w-2 rounded-full ${resources?.networkDisabled ? "bg-rose" : "bg-mint"}`} />
              Runtime network: {resources?.networkDisabled ? "disabled" : "enabled"}
            </div>
          </div>
        </Panel>
        <div className="xl:col-span-3" />
      </div>
    </div>
  );
}

function ResourceLimit({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-outline bg-surface px-3 py-2">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 font-mono text-sm font-semibold text-ink">{value}</p>
    </div>
  );
}

function ContainerUsageRow({ container }: { container: ContainerResourceUsage }) {
  return (
    <div className="rounded border border-outline bg-white p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-bold text-ink">{container.name}</p>
          <p className="mt-1 font-mono text-[11px] text-steel">{container.containerId}</p>
        </div>
        <span className="rounded bg-slate-100 px-2 py-1 font-mono text-[11px] font-semibold text-ink">
          {container.pids} pids
        </span>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-2 text-xs">
        <UsageMetric label="CPU" value={container.cpuPercent} />
        <UsageMetric label="Memory" value={`${container.memoryPercent} · ${container.memoryUsage}`} />
        <UsageMetric label="Network" value={container.networkIo} />
        <UsageMetric label="Block I/O" value={container.blockIo} />
      </div>
    </div>
  );
}

function UsageMetric({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 truncate font-mono text-[11px] font-semibold text-ink">{value || "-"}</p>
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
