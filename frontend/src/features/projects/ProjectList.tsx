import { Link } from "react-router-dom";
import { EmptyState } from "../../components/EmptyState";
import { GitHubRepoLink } from "../../components/GitHubRepoLink";
import { StackBadge } from "../../components/StackBadge";
import { JobStatusBadge } from "../jobs/JobStatusBadge";
import type { ProjectListItem } from "../jobs/types";

export function ProjectList({ projects }: { projects: ProjectListItem[] }) {
  if (projects.length === 0) {
    return <EmptyState title="No projects yet" description="Create the first application project from GitHub." />;
  }

  return (
    <div className="overflow-hidden rounded border border-outline">
      <table className="min-w-full border-collapse bg-white">
        <thead>
          <tr className="border-b border-outline bg-slate-50 text-left text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
            <th className="px-4 py-3">Project</th>
            <th className="px-4 py-3">Branch</th>
            <th className="px-4 py-3">Stack</th>
            <th className="px-4 py-3">Current</th>
            <th className="px-4 py-3">Deployment</th>
            <th className="px-4 py-3">Jobs</th>
            <th className="px-4 py-3 text-right">Action</th>
          </tr>
        </thead>
        <tbody className="text-sm">
          {projects.map((project) => (
            <tr key={project.id} className="border-b border-outline last:border-b-0 hover:bg-[rgba(211,228,254,0.3)]">
              <td className="px-4 py-3">
                <div className="flex items-center gap-2">
                  <div className="min-w-0">
                    <p className="font-medium text-secondary">{project.name}</p>
                    <p className="mt-0.5 truncate font-mono text-[11px] text-steel">{project.defaultProjectPath || "/"}</p>
                  </div>
                  <GitHubRepoLink href={project.repositoryUrl} />
                </div>
              </td>
              <td className="px-4 py-3">
                <span className="font-mono text-[12px] text-steel">{project.defaultBranch || "default"}</span>
              </td>
              <td className="px-4 py-3">
                <StackBadge stack={project.detectedStack} compact />
              </td>
              <td className="px-4 py-3">
                {project.currentStatus ? <JobStatusBadge status={project.currentStatus} /> : <span className="text-[12px] text-slate-400">not built</span>}
              </td>
              <td className="px-4 py-3">
                {project.deploymentUrl ? (
                  <div className="flex flex-col gap-1">
                    <a
                      className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink"
                      href={project.deploymentUrl}
                      target="_blank"
                      rel="noreferrer"
                    >
                      Open
                      <span className="material-symbols-outlined text-[14px]">open_in_new</span>
                    </a>
                    <span className="font-mono text-[11px] text-steel">
                      {project.publicHostname || (project.publishedPort ? `:${project.publishedPort}` : project.deploymentUrl)}
                    </span>
                  </div>
                ) : (
                  <span className="text-[12px] text-slate-400">pending</span>
                )}
              </td>
              <td className="px-4 py-3 font-mono text-[12px] text-steel">{project.jobsCount}</td>
              <td className="px-4 py-3 text-right">
                <Link className="inline-flex items-center gap-1 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink" to={`/projects/${project.id}`}>
                  Details
                  <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
                </Link>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
