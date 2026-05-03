type SecretStatusProps = {
  label: string;
  status: string;
};

export function SecretStatus({ label, status }: SecretStatusProps) {
  const normalized = status.replace(/_/g, "-");
  const className =
    normalized === "configured"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : normalized === "not-configured"
        ? "border-rose-200 bg-rose-50 text-rose-700"
        : "border-outline bg-surface-low text-steel";

  return (
    <div className="flex items-center justify-between gap-3 rounded border border-outline bg-white px-3 py-2">
      <span className="font-mono text-[12px] font-semibold text-ink">{label}</span>
      <span className={`inline-flex items-center rounded border px-2 py-1 text-[10px] font-bold uppercase tracking-[0.08em] ${className}`}>
        {normalized.replace("-", " ")}
      </span>
    </div>
  );
}
