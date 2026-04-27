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
    <div className="overflow-hidden rounded border border-outline bg-white">
      <div className="border-b border-outline bg-slate-50 px-4 py-3 text-sm font-semibold text-ink">{name}</div>
      <pre className="max-h-[28rem] overflow-auto bg-[#fafafa] p-4 font-mono text-[11px] leading-5 text-ink">{content}</pre>
    </div>
  );
}
