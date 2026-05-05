import { Link } from "react-router-dom";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { ProjectCreateForm } from "../features/projects/ProjectCreateForm";

export function ProjectCreatePage() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Applications"
        title="Create Project"
        description="Register a GitHub repository as a persistent application before launching builds."
      />

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_20rem]">
        <Panel title="Project Setup" description="A project stores repository defaults and groups future build jobs.">
          <ProjectCreateForm />
        </Panel>

        <Panel title="Project Flow">
          <div className="space-y-3 text-sm text-steel">
            <p>Create the application record first, then launch builds from the project details screen.</p>
            <Link
              to="/projects"
              className="inline-flex items-center gap-2 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink"
            >
              View Projects
              <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
            </Link>
          </div>
        </Panel>
      </div>
    </div>
  );
}
