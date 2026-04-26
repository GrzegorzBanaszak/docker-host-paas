type EmptyStateProps = {
  title: string;
  description: string;
};

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <div className="rounded-2xl border border-dashed border-slate-300 bg-slate-50 px-6 py-10 text-center">
      <h3 className="text-base font-semibold text-ink">{title}</h3>
      <p className="mt-2 text-sm text-steel">{description}</p>
    </div>
  );
}
