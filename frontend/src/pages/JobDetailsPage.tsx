import { Link, useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { JobActions } from "../features/jobs/JobActions";
import { JobDetailsCard } from "../features/jobs/JobDetailsCard";
import { useJob, useSystemResources } from "../features/jobs/hooks";
import { JobStatusBadge } from "../features/jobs/JobStatusBadge";
import { Icon } from "../components/Icon";
import { JobImageList } from "../features/images/JobImageList";
import type { ContainerResourceUsage, ContainerStatus, JobDetails, SystemResourceSnapshot } from "../features/jobs/types";

export function JobDetailsPage() {
  const { jobId = "" } = useParams();
  const jobQuery = useJob(jobId);
  const resourcesQuery = useSystemResources();

  if (!jobId) {
    return <EmptyState title="Missing job id" description="Open job details from the jobs list." />;
  }

  if (!jobQuery.data) {
    return <EmptyState title="Job not loaded" description="The requested job is not available yet or does not exist." />;
  }

  return (
    <div className="grid grid-cols-1 gap-4 xl:grid-cols-[minmax(18rem,22rem)_minmax(0,1fr)_minmax(18rem,20rem)]">
      <header className="xl:col-span-3 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="mb-1 flex items-center gap-3">
            <Link to="/jobs" className="flex items-center text-steel transition hover:text-ink">
              <Icon name="arrow_back" className="text-[16px]" />
            </Link>
            <h1 className="text-[20px] font-semibold tracking-[-0.01em] text-ink">{jobQuery.data.name}</h1>
            <JobStatusBadge status={jobQuery.data.status} />
            <ContainerStatusIndicator status={jobQuery.data.containerStatus} />
          </div>
          <div className="flex flex-wrap items-center gap-4 text-xs text-steel">
            <span className="flex items-center gap-1 font-mono text-[11px]">
              <Icon name="fingerprint" className="text-[14px]" />
              #{jobQuery.data.id.slice(0, 8)}
            </span>
            <span className="flex items-center gap-1">
              <Icon name="code" className="text-[14px]" />
              {jobQuery.data.repositoryUrl}
            </span>
            <span className="flex items-center gap-1">
              <Icon name="schedule" className="text-[14px]" />
              {new Date(jobQuery.data.createdAtUtc).toLocaleString()}
            </span>
            <span className="flex items-center gap-1">
              <Icon name="call_split" className="text-[14px]" />
              {jobQuery.data.branch || "main"}
            </span>
          </div>
        </div>
        <JobActions job={jobQuery.data} />
      </header>

      <div className="min-w-0 space-y-4">
        <JobDetailsCard job={jobQuery.data} />
      </div>

      <div className="min-w-0">
        <section className="rounded border border-outline bg-white p-4">
          <h3 className="mb-4 border-b border-outline pb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
            Image History
          </h3>
          <JobImageList
            images={jobQuery.data.images}
            selectedImageId={jobQuery.data.currentImageId ?? undefined}
            onSelect={() => undefined}
          />
        </section>
      </div>

      <div className="min-w-0">
        <section className="rounded border border-outline bg-white p-4">
          <h3 className="mb-4 border-b border-outline pb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
            Execution Pipeline
          </h3>
          <div className="relative space-y-5 pl-3 before:absolute before:bottom-2 before:left-[15px] before:top-2 before:w-px before:bg-outline before:content-['']">
            {buildTimeline(jobQuery.data).map((step) => (
              <div key={step.label} className="relative pl-4">
                <div className={`absolute -left-[7px] top-1 z-10 h-3 w-3 rounded-full border-2 border-white ${step.dotClass}`} />
                <h4 className={`text-sm ${step.isActive ? "font-semibold text-ink" : "font-medium text-steel"}`}>{step.label}</h4>
                <p className={`text-[11px] ${step.isActive ? "font-medium text-secondary" : "text-slate-400"}`}>{step.caption}</p>
              </div>
            ))}
          </div>
        </section>
        <div className="mt-4">
          <JobResourcePanel job={jobQuery.data} resources={resourcesQuery.data} />
        </div>
      </div>
    </div>
  );
}

function JobResourcePanel({ job, resources }: { job: JobDetails; resources?: SystemResourceSnapshot }) {
  const currentUsage = resources?.containers.find((container) => {
    if (job.containerId && container.containerId.startsWith(job.containerId.slice(0, 12))) {
      return true;
    }

    return Boolean(job.containerName && container.name === job.containerName);
  });

  return (
    <section className="rounded border border-outline bg-white p-4">
      <h3 className="mb-4 border-b border-outline pb-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
        Resource Guard
      </h3>
      <div className="space-y-4">
        <div className="grid grid-cols-3 gap-2">
          <ResourceLimit label="CPU" value={resources?.cpuLimit ?? "-"} />
          <ResourceLimit label="RAM" value={resources?.memoryLimit ?? "-"} />
          <ResourceLimit label="PIDs" value={resources?.pidsLimit ?? "-"} />
        </div>

        {resources?.status === "unavailable" ? (
          <p className="text-sm font-medium text-rose">{resources.errorMessage ?? "Docker stats unavailable."}</p>
        ) : currentUsage ? (
          <ContainerUsageDetails container={currentUsage} />
        ) : (
          <p className="text-sm text-steel">No live resource sample is available for this job container.</p>
        )}

        <div className="flex items-center gap-2 text-xs text-steel">
          <span className={`h-2 w-2 rounded-full ${resources?.networkDisabled ? "bg-rose" : "bg-mint"}`} />
          Runtime network: {resources?.networkDisabled ? "disabled" : "enabled"}
        </div>
      </div>
    </section>
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

function ContainerUsageDetails({ container }: { container: ContainerResourceUsage }) {
  return (
    <div className="rounded border border-outline bg-surface p-3">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-bold text-ink">{container.name}</p>
          <p className="mt-1 font-mono text-[11px] text-steel">{container.containerId}</p>
        </div>
        <span className="rounded bg-white px-2 py-1 font-mono text-[11px] font-semibold text-ink">
          {container.pids} pids
        </span>
      </div>
      <div className="mt-3 grid grid-cols-2 gap-3">
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

function ContainerStatusIndicator({ status }: { status?: ContainerStatus | null }) {
  const normalized = status ?? "not_found";
  const meta =
    normalized === "running"
      ? {
          label: "Container running",
          detail: "Container is up and responding.",
          iconClass: "text-emerald-600",
          dotClass: "bg-emerald-500"
        }
      : normalized === "restarting"
        ? {
            label: "Container restarting",
            detail: "Docker is restarting the current container.",
            iconClass: "text-sky-600",
            dotClass: "bg-sky-500"
          }
        : normalized === "paused"
          ? {
              label: "Container paused",
              detail: "Container exists but is currently paused.",
              iconClass: "text-amber-600",
              dotClass: "bg-amber-500"
            }
          : normalized === "created"
            ? {
                label: "Container created",
                detail: "Container exists but has not started yet.",
                iconClass: "text-slate-600",
                dotClass: "bg-slate-400"
              }
            : normalized === "exited" || normalized === "dead"
              ? {
                  label: "Container stopped",
                  detail: "Container is no longer running.",
                  iconClass: "text-rose-600",
                  dotClass: "bg-rose-500"
                }
              : {
                  label: "Container not found",
                  detail: "No active container is attached to this job.",
                  iconClass: "text-steel",
                  dotClass: "bg-slate-300"
                };

  return (
    <div className="group relative">
      <button
        type="button"
        className="inline-flex h-7 w-7 items-center justify-center rounded-full border border-outline bg-white transition hover:border-slate-300 hover:bg-slate-50 focus-visible:border-slate-400 focus-visible:outline-none"
        aria-label={meta.label}
        title={`${meta.label}. ${meta.detail}`}
      >
        <span className="relative flex h-4 w-4 items-center justify-center">
          <Icon name="deployed_code" className={`text-[16px] ${meta.iconClass}`} />
          <span className={`absolute -bottom-0.5 -right-0.5 h-1.5 w-1.5 rounded-full border border-white ${meta.dotClass}`} />
        </span>
      </button>
      <div className="pointer-events-none absolute left-1/2 top-[calc(100%+0.5rem)] z-10 w-52 -translate-x-1/2 rounded border border-outline bg-white px-3 py-2 opacity-0 shadow-sm transition group-hover:opacity-100 group-focus-within:opacity-100">
        <p className="text-[11px] font-bold uppercase tracking-[0.08em] text-ink">{meta.label}</p>
        <p className="mt-1 text-[11px] leading-4 text-steel">{meta.detail}</p>
      </div>
    </div>
  );
}

function buildTimeline(job: {
  status: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  detectedStack?: string | null;
  generatedImageTag?: string | null;
  imageId?: string | null;
  deploymentUrl?: string | null;
  deployedAtUtc?: string | null;
}) {
  const doneClass = "bg-emerald-500";
  const activeClass = "bg-sky-500 ring-2 ring-sky-200";
  const pendingClass = "bg-white border-outline";
  const failedClass = "bg-rose-600";
  const canceledClass = "bg-amber-500";

  const isFinished = job.status === "Succeeded";
  const isFailed = job.status === "Failed";
  const isCanceled = job.status === "Canceled";
  const buildFinished = Boolean(job.imageId || job.generatedImageTag);
  const deployFinished = Boolean(job.deploymentUrl);
  const isRunningDeploy = job.status === "Running" && buildFinished && !deployFinished;

  return [
    { label: "Queued", caption: "Initial state", dotClass: doneClass, isActive: true },
    { label: "Clone Repository", caption: job.startedAtUtc ? "Completed" : "Pending", dotClass: job.startedAtUtc ? doneClass : pendingClass, isActive: !!job.startedAtUtc },
    { label: "Analyze Context", caption: job.detectedStack ? `Detected ${job.detectedStack}` : "Pending", dotClass: job.detectedStack ? doneClass : pendingClass, isActive: !!job.detectedStack },
    { label: "Generate Dockerfile", caption: job.detectedStack ? "Completed" : "Pending", dotClass: job.detectedStack ? doneClass : pendingClass, isActive: !!job.detectedStack },
    {
      label: "Build Image",
      caption: buildFinished ? "Completed" : job.status === "Running" ? "In Progress..." : isFailed ? "Failed" : isCanceled ? "Canceled" : "Pending",
      dotClass: buildFinished ? doneClass : job.status === "Running" ? activeClass : isFailed ? failedClass : isCanceled ? canceledClass : pendingClass,
      isActive: buildFinished || job.status === "Running" || isFailed || isCanceled
    },
    {
      label: "Deploy Container",
      caption: deployFinished ? "Container is reachable" : isRunningDeploy ? "Starting container..." : isFailed ? "Failed" : isCanceled ? "Canceled" : "Pending",
      dotClass: deployFinished ? doneClass : isRunningDeploy ? activeClass : isFailed ? failedClass : isCanceled ? canceledClass : pendingClass,
      isActive: deployFinished || isRunningDeploy || isFailed || isCanceled
    },
    {
      label: "Deployment Ready",
      caption: isFinished ? job.deploymentUrl ?? "Ready" : isFailed ? "Failed" : isCanceled ? "Canceled" : "Pending",
      dotClass: isFinished ? doneClass : isFailed ? failedClass : isCanceled ? canceledClass : pendingClass,
      isActive: !!job.completedAtUtc
    }
  ];
}
