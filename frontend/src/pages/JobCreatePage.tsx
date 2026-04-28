import { Link } from "react-router-dom";
import { Panel } from "../components/Panel";
import { PageHeader } from "../components/PageHeader";
import { JobCreateForm } from "../features/jobs/JobCreateForm";

export function JobCreatePage() {
  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Operations"
        title="Create Job"
        description="Name the pipeline, choose the repository, and optionally lock it to a specific branch."
      />

      <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_20rem]">
        <Panel title="Job Setup" description="Branch options are loaded directly from the repository before the job enters the queue.">
          <JobCreateForm />
        </Panel>

        <Panel title="Queue Flow">
          <div className="space-y-3 text-sm text-steel">
            <p>The worker clones the selected branch, generates container files, stores build artifacts in the database, and then starts the container.</p>
            <Link
              to="/jobs"
              className="inline-flex items-center gap-2 text-[12px] font-semibold uppercase tracking-[0.08em] text-secondary hover:text-ink"
            >
              View Existing Jobs
              <span className="material-symbols-outlined text-[14px]">arrow_forward</span>
            </Link>
          </div>
        </Panel>
      </div>
    </div>
  );
}
