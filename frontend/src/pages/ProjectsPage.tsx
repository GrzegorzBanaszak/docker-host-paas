import { Link } from "react-router-dom";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { ProjectList } from "../features/projects/ProjectList";
import { useProjects } from "../features/jobs/hooks";

export function ProjectsPage() {
  const projectsQuery = useProjects();
  const projects = projectsQuery.data ?? [];

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Applications"
        title="Projects"
        description="Manage persistent applications, their build history, and current deployments."
      />

      <div className="flex justify-end">
        <Link
          to="/projects/new"
          className="inline-flex h-10 items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 text-[11px] font-bold uppercase tracking-[0.14em] text-white transition hover:bg-slate-800"
        >
          <span className="material-symbols-outlined text-[18px]">add</span>
          New Project
        </Link>
      </div>

      <Panel>
        <ProjectList projects={projects} />
      </Panel>
    </div>
  );
}
