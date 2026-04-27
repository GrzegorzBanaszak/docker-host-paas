type PageHeaderProps = {
  eyebrow: string;
  title: string;
  description: string;
};

export function PageHeader({ eyebrow, title, description }: PageHeaderProps) {
  return (
    <div className="mb-6">
      <p className="mb-2 text-[11px] font-bold uppercase tracking-[0.24em] text-slate-500">{eyebrow}</p>
      <h1 className="text-[32px] font-bold tracking-[-0.02em] text-ink">{title}</h1>
      <p className="mt-1 max-w-3xl text-sm leading-5 text-steel">{description}</p>
    </div>
  );
}
