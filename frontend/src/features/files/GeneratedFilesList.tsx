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
    <div className="border-b border-outline bg-white">
      {files.map((file) => (
        <button
          key={file.id}
          type="button"
          onClick={() => onSelect(file.id)}
          className={`flex w-full items-center gap-2 border-l-2 px-3 py-2 text-left text-sm transition ${
            selectedFileId === file.id
              ? "border-l-slate-900 bg-variant font-medium text-ink"
              : "border-l-transparent text-steel hover:bg-slate-50"
          }`}
        >
          <span className="material-symbols-outlined text-[16px]">description</span>
          {file.name}
        </button>
      ))}
    </div>
  );
}
