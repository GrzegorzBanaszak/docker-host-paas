type RouteStatusBadgeProps = {
  status?: string | null;
};

export function RouteStatusBadge({ status }: RouteStatusBadgeProps) {
  const normalized = status ?? "pending";
  const className =
    normalized === "reverse-proxy-configured"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : normalized === "port-published"
        ? "border-sky-200 bg-sky-50 text-sky-700"
        : normalized === "failed"
          ? "border-rose-200 bg-rose-50 text-rose-700"
      : normalized === "unknown"
        ? "border-outline bg-surface-low text-steel"
        : normalized === "private"
          ? "border-slate-200 bg-slate-50 text-slate-700"
          : "border-amber-200 bg-amber-50 text-amber-700";

  return (
    <span className={`inline-flex items-center gap-1.5 rounded border px-2 py-1 text-[11px] font-bold uppercase tracking-[0.08em] ${className}`}>
      <span className="h-1.5 w-1.5 rounded-full bg-current opacity-80" />
      {normalized.replace(/-/g, " ")}
    </span>
  );
}
