import type { ReactNode } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { GitHubRepoLink } from "../components/GitHubRepoLink";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { StackBadge } from "../components/StackBadge";
import { GeneratedFilesList } from "../features/files/GeneratedFilesList";
import { JobImageList } from "../features/images/JobImageList";
import { JobList } from "../features/jobs/JobList";
import { JobStatusBadge } from "../features/jobs/JobStatusBadge";
import {
  useArchiveProject,
  useCreateProjectJob,
  useJobFiles,
  useJobLogs,
  useProject,
  usePublishProject,
  useSystemResources,
  useUnpublishProject
} from "../features/jobs/hooks";
import { LogViewer } from "../features/logs/LogViewer";
import type { ContainerResourceUsage, ProjectDetails, SystemResourceSnapshot } from "../features/jobs/types";

export function ProjectDetailsPage() {
  const { projectId = "" } = useParams();
  const navigate = useNavigate();
  const projectQuery = useProject(projectId);
  const resourcesQuery = useSystemResources();
  const createJob = useCreateProjectJob(projectId);
  const publishProject = usePublishProject(projectId);
  const unpublishProject = useUnpublishProject(projectId);
  const archiveProject = useArchiveProject(projectId);
  const project = projectQuery.data;
  const currentJobId = project?.currentJobId ?? "";
  const logsQuery = useJobLogs(currentJobId);
  const filesQuery = useJobFiles(currentJobId);

  if (!project && projectQuery.isLoading) {
    return <EmptyState title="Loading project" description="Project details are being loaded." />;
  }

  if (!project) {
    return <EmptyState title="Project not found" description="The selected project does not exist." />;
  }

  async function handleBuild() {
    await createJob.mutateAsync({});
  }

  async function handleArchive() {
    await archiveProject.mutateAsync();
    navigate("/projects");
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <PageHeader
          eyebrow="Project"
          title={project.name}
          description={`${project.repositoryUrl}${project.defaultProjectPath ? ` / ${project.defaultProjectPath}` : ""}`}
        />
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => void handleBuild()}
            disabled={createJob.isPending}
            className="inline-flex h-10 items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className={`material-symbols-outlined text-[18px] ${createJob.isPending ? "animate-spin" : ""}`}>{createJob.isPending ? "progress_activity" : "play_arrow"}</span>
            Build
          </button>
          {project.publicAccessEnabled ? (
            <button
              type="button"
              onClick={() => unpublishProject.mutate()}
              disabled={unpublishProject.isPending}
              className="inline-flex h-10 items-center justify-center gap-2 rounded border border-outline bg-white px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <span className="material-symbols-outlined text-[18px]">lock</span>
              Unpublish
            </button>
          ) : (
            <button
              type="button"
              onClick={() => publishProject.mutate()}
              disabled={publishProject.isPending || !project.currentJobId}
              className="inline-flex h-10 items-center justify-center gap-2 rounded border border-outline bg-white px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-ink transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <span className="material-symbols-outlined text-[18px]">public</span>
              Publish
            </button>
          )}
          <button
            type="button"
            onClick={() => void handleArchive()}
            disabled={archiveProject.isPending}
            className="inline-flex h-10 items-center justify-center gap-2 rounded border border-rose-300 bg-rose-50 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-rose-700 transition hover:bg-rose-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            <span className="material-symbols-outlined text-[18px]">archive</span>
            Archive
          </button>
        </div>
      </div>

      <section className="grid gap-4 xl:grid-cols-4">
        <StatusTile label="Current Status">
          {project.currentStatus ? <JobStatusBadge status={project.currentStatus} /> : <span className="text-sm text-steel">Not built</span>}
        </StatusTile>
        <StatusTile label="Stack">
          <StackBadge stack={project.detectedStack} />
        </StatusTile>
        <StatusTile label="Branch">
          <span className="font-mono text-sm font-semibold text-ink">{project.defaultBranch || "default"}</span>
        </StatusTile>
        <StatusTile label="Repository">
          <div className="flex items-center gap-2">
            <span className="truncate font-mono text-xs text-steel">{formatRepositoryLabel(project.repositoryUrl)}</span>
            <GitHubRepoLink href={project.repositoryUrl} />
          </div>
        </StatusTile>
      </section>

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <Panel title="Deployment" description="Current successful job and public route state.">
          <div className="space-y-3 text-sm">
            {project.deploymentUrl ? (
              <a
                href={project.deploymentUrl}
                target="_blank"
                rel="noreferrer"
                className="inline-flex items-center gap-2 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink"
              >
                Open Deployment
                <span className="material-symbols-outlined text-[14px]">open_in_new</span>
              </a>
            ) : (
              <p className="text-steel">No deployment URL is assigned yet.</p>
            )}
            <dl className="grid gap-2 md:grid-cols-2">
              <Meta label="Hostname" value={project.publicHostname || "-"} />
              <Meta label="Route" value={project.routeStatus || "-"} />
              <Meta label="Container" value={project.containerName || "-"} />
              <Meta label="Image" value={project.generatedImageTag || "-"} />
            </dl>
          </div>
        </Panel>

        <Panel title="Current Image">
          {project.currentImage ? (
            <JobImageList images={[project.currentImage]} selectedImageId={project.currentImage.id} onSelect={() => undefined} />
          ) : (
            <EmptyState title="No current image" description="Run the first build to create an image." />
          )}
        </Panel>
      </div>

      <ProjectResourcePanel project={project} resources={resourcesQuery.data} />

      <Panel title="Build History" description="Jobs launched from this project.">
        <JobList jobs={project.jobs} mode="full" />
      </Panel>

      <div className="grid gap-4 xl:grid-cols-[22rem_minmax(0,1fr)]">
        <Panel title="Generated Files" description="Files captured from the current job.">
          <GeneratedFilesList files={filesQuery.data ?? []} onSelect={() => undefined} />
        </Panel>
        <Panel title="Current Job Logs">
          <LogViewer content={logsQuery.data?.content} />
        </Panel>
      </div>

      <div>
        <Link className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink" to="/projects">
          <span className="material-symbols-outlined text-[14px]">arrow_back</span>
          Back to projects
        </Link>
      </div>
    </div>
  );
}

function ProjectResourcePanel({ project, resources }: { project: ProjectDetails; resources?: SystemResourceSnapshot }) {
  const currentUsage = resources?.containers.find((container) => Boolean(project.containerName && container.name === project.containerName));

  return (
    <Panel title="Resource Guard" description="Live Docker resource usage for the current project container.">
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="grid flex-1 grid-cols-3 gap-2">
            <ResourceLimit label="CPU" value={resources?.cpuLimit ?? "-"} />
            <ResourceLimit label="RAM" value={resources?.memoryLimit ?? "-"} />
            <ResourceLimit label="PIDs" value={resources?.pidsLimit ?? "-"} />
          </div>
          <div className="flex items-center gap-2 rounded border border-outline bg-surface px-2 py-1 text-[11px] font-semibold text-steel">
            <span className={`h-2 w-2 rounded-full ${resources?.networkDisabled ? "bg-rose" : "bg-mint"}`} />
            Network {resources?.networkDisabled ? "off" : "on"}
          </div>
        </div>

        {resources?.status === "unavailable" ? (
          <p className="text-sm font-medium text-rose">{resources.errorMessage ?? "Docker stats unavailable."}</p>
        ) : currentUsage ? (
          <ContainerUsageDetails container={currentUsage} />
        ) : (
          <p className="text-sm text-steel">No live resource sample is available for the current project container.</p>
        )}
      </div>
    </Panel>
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
  const cpuPercent = parsePercent(container.cpuPercent);
  const memoryPercent = parsePercent(container.memoryPercent);

  return (
    <div className="rounded border border-outline bg-surface p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-sm font-bold text-ink">{container.name}</p>
          <p className="mt-1 font-mono text-[11px] text-steel">{container.containerId}</p>
        </div>
        <span className="rounded bg-white px-2 py-1 font-mono text-[11px] font-semibold text-ink">
          {container.pids} pids
        </span>
      </div>
      <div className="mt-4 space-y-4">
        <UsageBar label="CPU" value={container.cpuPercent} percent={cpuPercent} tone="bg-sky-500" />
        <UsageBar label="Memory" value={`${container.memoryPercent} / ${container.memoryUsage}`} percent={memoryPercent} tone="bg-emerald-500" />
      </div>
      <div className="mt-4 grid gap-3 md:grid-cols-2">
        <UsageMetric label="Network I/O" value={container.networkIo} />
        <UsageMetric label="Block I/O" value={container.blockIo} />
      </div>
    </div>
  );
}

function UsageMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-outline bg-white px-3 py-2">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 truncate font-mono text-[11px] font-semibold text-ink">{value || "-"}</p>
    </div>
  );
}

function UsageBar({ label, value, percent, tone }: { label: string; value: string; percent: number | null; tone: string }) {
  const width = percent === null ? 0 : Math.min(Math.max(percent, 0), 100);

  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between gap-3">
        <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
        <p className="font-mono text-[11px] font-semibold text-ink">{value || "-"}</p>
      </div>
      <div className="h-2.5 overflow-hidden rounded-full border border-outline bg-white">
        <div className={`h-full rounded-full ${tone}`} style={{ width: `${width}%` }} />
      </div>
    </div>
  );
}

function parsePercent(value: string) {
  const parsed = Number.parseFloat(value.replace("%", "").replace(",", "."));

  return Number.isFinite(parsed) ? parsed : null;
}

function StatusTile({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="rounded border border-outline bg-white p-4">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <div className="mt-2 min-h-7">{children}</div>
    </div>
  );
}

function Meta({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-outline bg-surface px-3 py-2">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 truncate font-mono text-[11px] font-semibold text-ink">{value}</p>
    </div>
  );
}

function formatRepositoryLabel(repositoryUrl: string) {
  try {
    return new URL(repositoryUrl).pathname.replace(/^\/+/, "") || repositoryUrl;
  } catch {
    return repositoryUrl;
  }
}
