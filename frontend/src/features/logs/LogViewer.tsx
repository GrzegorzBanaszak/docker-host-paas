import { EmptyState } from "../../components/EmptyState";

export function LogViewer({ content }: { content?: string }) {
  if (!content) {
    return <EmptyState title="No logs yet" description="Logs will appear here when the worker starts processing the job." />;
  }

  return (
    <pre className="max-h-[28rem] overflow-auto rounded-2xl bg-ink p-4 font-mono text-xs leading-6 text-mist">
      {content}
    </pre>
  );
}
