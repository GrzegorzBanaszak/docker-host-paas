import { EmptyState } from "../../components/EmptyState";

type FilePreviewPanelProps = {
  name?: string;
  content?: string;
};

export function FilePreviewPanel({ name, content }: FilePreviewPanelProps) {
  if (!name || !content) {
    return <EmptyState title="Select a file" description="Choose a generated artifact to inspect its contents." />;
  }

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200">
      <div className="border-b border-slate-200 bg-slate-50 px-4 py-3 text-sm font-semibold text-ink">{name}</div>
      <pre className="max-h-[28rem] overflow-auto bg-white p-4 font-mono text-xs leading-6 text-ink">{content}</pre>
    </div>
  );
}
