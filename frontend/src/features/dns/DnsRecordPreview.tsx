type DnsRecordPreviewProps = {
  type: string;
  name: string;
  target: string;
  proxyStatus?: string;
};

export function DnsRecordPreview({ type, name, target, proxyStatus = "unknown" }: DnsRecordPreviewProps) {
  return (
    <div className="grid gap-2 rounded border border-outline bg-surface p-3 text-sm sm:grid-cols-[80px_1fr_1.5fr_110px]">
      <RecordCell label="Type" value={type} />
      <RecordCell label="Name" value={name} />
      <RecordCell label="Target" value={target} />
      <RecordCell label="Proxy" value={proxyStatus} />
    </div>
  );
}

function RecordCell({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 truncate font-mono text-[12px] font-semibold text-ink" title={value}>
        {value}
      </p>
    </div>
  );
}
