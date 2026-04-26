import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { FilePreviewPanel } from "../features/files/FilePreviewPanel";
import { GeneratedFilesList } from "../features/files/GeneratedFilesList";
import { JobActions } from "../features/jobs/JobActions";
import { JobDetailsCard } from "../features/jobs/JobDetailsCard";
import { useJob, useJobFileContent, useJobFiles, useJobLogs } from "../features/jobs/hooks";
import { LogViewer } from "../features/logs/LogViewer";

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
    <div className="space-y-8">
      <PageHeader
        eyebrow="Inspection"
        title="Job details"
        description="Monitor status, inspect generated files and review backend execution logs for a specific job."
      />

      <Panel title="Execution summary" description="Core job metadata and current processing result.">
        <div className="space-y-5">
          <JobActions job={jobQuery.data} />
          <JobDetailsCard job={jobQuery.data} />
        </div>
      </Panel>

      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <Panel title="Logs" description="Polled from the backend worker output file.">
          <LogViewer content={logsQuery.data?.content} />
        </Panel>

        <Panel title="Generated files" description="Inspect Docker artifacts created in the repository workspace.">
          <div className="space-y-5">
            <GeneratedFilesList
              files={filesQuery.data ?? []}
              selectedFileId={activeFileId}
              onSelect={setSelectedFileId}
            />
            <FilePreviewPanel
              name={fileContentQuery.data?.name}
              content={fileContentQuery.data?.content}
            />
          </div>
        </Panel>
      </div>
    </div>
  );
}
