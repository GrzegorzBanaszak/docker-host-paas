import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { FilePreviewPanel } from "../features/files/FilePreviewPanel";
import { GeneratedFilesList } from "../features/files/GeneratedFilesList";
import { ImageDetailsCard } from "../features/images/ImageDetailsCard";
import { useImage, useImageFileContent, useImageFiles, useImageLogs } from "../features/jobs/hooks";
import { LogViewer } from "../features/logs/LogViewer";
import { Icon } from "../components/Icon";

export function ImageDetailsPage() {
  const { imageId = "" } = useParams();
  const [selectedFileId, setSelectedFileId] = useState<string>();

  const imageQuery = useImage(imageId);
  const logsQuery = useImageLogs(imageId);
  const filesQuery = useImageFiles(imageId);

  const defaultFileId = useMemo(() => filesQuery.data?.[0]?.id, [filesQuery.data]);
  const activeFileId = selectedFileId ?? defaultFileId;
  const fileContentQuery = useImageFileContent(imageId, activeFileId);

  if (!imageId) {
    return <EmptyState title="Missing image id" description="Open image details from the images list." />;
  }

  if (!imageQuery.data) {
    return <EmptyState title="Image not loaded" description="The requested image is not available yet or does not exist." />;
  }

  return (
    <div className="grid grid-cols-1 gap-4 xl:grid-cols-[minmax(18rem,22rem)_minmax(0,1fr)_minmax(18rem,20rem)]">
      <header className="xl:col-span-3 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="mb-1 flex items-center gap-3">
            <Link to="/images" className="flex items-center text-steel transition hover:text-ink">
              <Icon name="arrow_back" className="text-[16px]" />
            </Link>
            <h1 className="text-[20px] font-semibold tracking-[-0.01em] text-ink">Image #{imageQuery.data.id.slice(0, 8)}</h1>
          </div>
          <div className="flex flex-wrap items-center gap-4 text-xs text-steel">
            <span className="flex items-center gap-1">
              <Icon name="terminal" className="text-[14px]" />
              <Link to={`/jobs/${imageQuery.data.jobId}`} className="hover:text-ink">
                {imageQuery.data.jobName}
              </Link>
            </span>
            <span className="flex items-center gap-1">
              <Icon name="call_split" className="text-[14px]" />
              {imageQuery.data.branch || "main"}
            </span>
            <span className="flex items-center gap-1">
              <Icon name="schedule" className="text-[14px]" />
              {new Date(imageQuery.data.createdAtUtc).toLocaleString()}
            </span>
          </div>
        </div>
        {imageQuery.data.jobDeploymentUrl ? (
          <a
            href={imageQuery.data.jobDeploymentUrl}
            target="_blank"
            rel="noreferrer"
            className="flex items-center gap-1 rounded border border-secondary bg-white px-3 py-1.5 text-sm font-medium text-secondary transition hover:bg-[rgba(211,228,254,0.35)]"
          >
            <span className="material-symbols-outlined text-[16px]">open_in_new</span>
            Open Deployment
          </a>
        ) : null}
      </header>

      <div className="min-w-0">
        <ImageDetailsCard image={imageQuery.data} />
      </div>

      <div className="min-w-0">
        {logsQuery.isError ? (
          <EmptyState title="Logs unavailable" description={(logsQuery.error as Error).message || "Could not load image logs."} />
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
