import type { PropsWithChildren } from "react";

type PanelProps = PropsWithChildren<{
  title?: string;
  description?: string;
}>;

export function Panel({ title, description, children }: PanelProps) {
  return (
    <section className="rounded border border-outline bg-white shadow-panel">
      {title || description ? (
        <div className="border-b border-outline bg-gradient-to-b from-white to-slate-50 px-4 py-3">
          {title ? <h2 className="text-[20px] font-semibold tracking-[-0.01em] text-ink">{title}</h2> : null}
          {description ? <p className="mt-1 text-sm text-steel">{description}</p> : null}
        </div>
      ) : null}
      <div className="p-4">{children}</div>
    </section>
  );
}
