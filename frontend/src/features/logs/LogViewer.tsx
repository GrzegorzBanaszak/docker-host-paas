import { EmptyState } from "../../components/EmptyState";

export function LogViewer({ content }: { content?: string }) {
  if (!content) {
    return <EmptyState title="No logs yet" description="Logs will appear here when the worker starts processing the job." />;
  }

  const lineCount = content.split(/\r?\n/).filter(Boolean).length;

  return (
    <div className="w-full min-w-0 max-w-full overflow-hidden rounded border border-[#333] bg-[#0a0a0a] shadow-panel">
      <div className="flex items-center justify-between border-b border-[#333] bg-[#1a1a1a] px-4 py-2">
        <div className="flex items-center gap-2 text-[11px] font-bold uppercase tracking-[0.12em] text-slate-300">
          <span className="material-symbols-outlined text-[16px]">terminal</span>
          Build Logs
        </div>
        <div className="flex items-center gap-2">
          <div className="inline-flex items-center rounded-sm border border-slate-700 px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.08em] text-slate-400">
            {lineCount} lines
          </div>
          <div className="inline-flex items-center gap-1.5 rounded-sm border border-sky-500/30 bg-sky-900/30 px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.08em] text-sky-300">
            <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-sky-400" />
            Live
          </div>
        </div>
      </div>
      <pre className="block max-h-[36rem] w-full min-w-0 max-w-full overflow-y-auto overflow-x-hidden whitespace-pre-wrap break-all bg-[#0a0a0a] p-4 font-mono text-[13px] leading-6 text-slate-300">{content}</pre>
    </div>
  );
}
