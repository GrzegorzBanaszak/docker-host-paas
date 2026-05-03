import { useState } from "react";
import { Icon } from "../../components/Icon";

type HostnameLinkProps = {
  hostname?: string | null;
  url?: string | null;
};

export function HostnameLink({ hostname, url }: HostnameLinkProps) {
  const [copied, setCopied] = useState(false);
  const displayValue = hostname || url;

  async function handleCopy() {
    if (!displayValue || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(url || displayValue);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  if (!displayValue) {
    return <span className="text-[12px] text-slate-400">pending</span>;
  }

  return (
    <div className="flex min-w-0 items-center gap-2">
      {url ? (
        <a className="min-w-0 truncate font-mono text-[12px] font-semibold text-secondary underline-offset-2 hover:underline" href={url} target="_blank" rel="noreferrer">
          {displayValue}
        </a>
      ) : (
        <span className="min-w-0 truncate font-mono text-[12px] font-semibold text-ink">{displayValue}</span>
      )}
      <button
        type="button"
        onClick={() => void handleCopy()}
        className="inline-flex h-7 w-7 flex-none items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink"
        title={copied ? "Copied" : "Copy URL"}
        aria-label={copied ? "Copied" : "Copy URL"}
      >
        <Icon name={copied ? "check" : "content_copy"} className="text-[15px]" />
      </button>
    </div>
  );
}
