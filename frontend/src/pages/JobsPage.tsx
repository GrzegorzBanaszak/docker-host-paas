import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { JobList } from "../features/jobs/JobList";
import { useJobs } from "../features/jobs/hooks";

export function JobsPage() {
  const jobsQuery = useJobs();

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Queue overview"
        title="All jobs"
        description="Browse current and historical containerization jobs with auto-refresh from the backend."
      />

      <Panel title="Job list" description="Repository, stack, status and creation time in one place.">
        <JobList jobs={jobsQuery.data ?? []} />
      </Panel>
    </div>
  );
}
