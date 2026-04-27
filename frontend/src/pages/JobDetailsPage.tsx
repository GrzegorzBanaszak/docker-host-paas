import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { FilePreviewPanel } from "../features/files/FilePreviewPanel";
import { GeneratedFilesList } from "../features/files/GeneratedFilesList";
import { JobActions } from "../features/jobs/JobActions";
import { JobDetailsCard } from "../features/jobs/JobDetailsCard";
import { useJob, useJobFileContent, useJobFiles, useJobLogs } from "../features/jobs/hooks";
import { JobStatusBadge } from "../features/jobs/JobStatusBadge";
import { LogViewer } from "../features/logs/LogViewer";
import { Icon } from "../components/Icon";

export function JobDetailsPage() {
  const { jobId = "" } = useParams();
  const [selectedFileId, setSelectedFileId] = useState<string>();

  const jobQuery = useJob(jobId);
  const logsQuery = useJobLogs(jobId);
  const filesQuery = useJobFiles(jobId);

  const defaultFileId = useMemo(() => filesQuery.data?.[0]?.id, [filesQuery.data]);
  const activeFileId = selectedFileId ?? defaultFileId;
  const fileContentQuery = useJobFileContent(jobId, activeFileId);

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
            <h1 className="text-[20px] font-semibold tracking-[-0.01em] text-ink">
              Job <span className="font-mono text-steel">#{jobQuery.data.id.slice(0, 8)}</span>
            </h1>
            <JobStatusBadge status={jobQuery.data.status} />
          </div>
          <div className="flex flex-wrap items-center gap-4 text-xs text-steel">
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
      </div>

      <div className="min-w-0">
        {logsQuery.isError ? (
          <EmptyState title="Logs unavailable" description={(logsQuery.error as Error).message || "Could not load job logs."} />
        ) : (
          <LogViewer content={logsQuery.data?.content} />
        )}
      </div>

      <div className="min-w-0">
        <div className="flex h-[600px] flex-col overflow-hidden rounded border border-outline bg-white">
        <div className="border-b border-outline bg-slate-50 px-3 py-2 text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
          Generated Files
        </div>
        <div className="flex flex-1 flex-col">
          <GeneratedFilesList
            files={filesQuery.data ?? []}
            selectedFileId={activeFileId}
            onSelect={setSelectedFileId}
          />
          <div className="flex-1">
            {fileContentQuery.isError ? (
              <EmptyState title="File unavailable" description={(fileContentQuery.error as Error).message || "Could not load the selected artifact."} />
            ) : (
              <FilePreviewPanel name={fileContentQuery.data?.name} content={fileContentQuery.data?.content} />
            )}
          </div>
        </div>
        </div>
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
  const buildFinished = Boolean(job.generatedImageTag);
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
      caption: isFinished ? (job.deploymentUrl ?? "Ready") : isFailed ? "Failed" : isCanceled ? "Canceled" : "Pending",
      dotClass: isFinished ? doneClass : isFailed ? failedClass : isCanceled ? canceledClass : pendingClass,
      isActive: !!job.completedAtUtc
    }
  ];
}
