const STACK_META: Record<string, { label: string; icon: string }> = {
  nodejs: { label: "Node.js", icon: "deployed_code" },
  python: { label: "Python", icon: "smart_toy" },
  php: { label: "PHP", icon: "code_blocks" },
  go: { label: "Go", icon: "route" },
  java: { label: "Java", icon: "local_cafe" },
  dotnet: { label: ".NET", icon: "data_object" },
  "dockerfile-only": { label: "Dockerfile", icon: "inventory_2" },
  "static-html": { label: "Static HTML", icon: "html" },
  unknown: { label: "Unknown", icon: "help" }
};

export function StackBadge({ stack, compact = false }: { stack?: string | null; compact?: boolean }) {
  const meta = STACK_META[stack ?? ""] ?? STACK_META.unknown;

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded border border-outline bg-surface-low px-2 py-1 font-medium text-steel ${
        compact ? "text-[11px]" : "text-xs"
      }`}
      title={meta.label}
    >
      <span className="material-symbols-outlined text-[14px]">{meta.icon}</span>
      <span>{stack ? meta.label : "pending"}</span>
    </span>
  );
}
