import type { JobFile } from "../jobs/types";
import { EmptyState } from "../../components/EmptyState";

type GeneratedFilesListProps = {
  files: JobFile[];
  selectedFileId?: string;
  onSelect: (fileId: string) => void;
};

export function GeneratedFilesList({ files, selectedFileId, onSelect }: GeneratedFilesListProps) {
  if (files.length === 0) {
    return <EmptyState title="No generated files" description="Generated Docker artifacts will be listed here." />;
  }

  return (
    <div className="flex flex-wrap gap-3">
      {files.map((file) => (
        <button
          key={file.id}
          type="button"
          onClick={() => onSelect(file.id)}
          className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
            selectedFileId === file.id ? "bg-ink text-white" : "bg-slate-100 text-steel hover:bg-slate-200"
          }`}
        >
          {file.name}
        </button>
      ))}
    </div>
  );
}
