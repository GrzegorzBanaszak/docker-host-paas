type PageHeaderProps = {
  eyebrow: string;
  title: string;
  description: string;
};

export function PageHeader({ eyebrow, title, description }: PageHeaderProps) {
  return (
    <div className="mb-8">
      <p className="mb-2 text-xs font-semibold uppercase tracking-[0.24em] text-coral">{eyebrow}</p>
      <h1 className="text-4xl font-semibold tracking-tight text-ink">{title}</h1>
      <p className="mt-3 max-w-3xl text-sm leading-6 text-steel">{description}</p>
    </div>
  );
}
