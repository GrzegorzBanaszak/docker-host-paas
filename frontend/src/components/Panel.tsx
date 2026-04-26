import type { PropsWithChildren } from "react";

type PanelProps = PropsWithChildren<{
  title?: string;
  description?: string;
}>;

export function Panel({ title, description, children }: PanelProps) {
  return (
    <section className="rounded-3xl border border-white/80 bg-white/90 p-6 shadow-panel">
      {title ? <h2 className="text-lg font-semibold text-ink">{title}</h2> : null}
      {description ? <p className="mt-1 text-sm text-steel">{description}</p> : null}
      <div className={title || description ? "mt-5" : ""}>{children}</div>
    </section>
  );
}
