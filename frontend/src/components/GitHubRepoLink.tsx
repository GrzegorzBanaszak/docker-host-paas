type GitHubRepoLinkProps = {
  href: string;
  className?: string;
};

export function GitHubRepoLink({ href, className }: GitHubRepoLinkProps) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      title={href}
      className={`inline-flex h-7 w-7 shrink-0 items-center justify-center rounded border border-outline bg-white text-steel transition hover:border-slate-300 hover:bg-slate-50 hover:text-ink ${className ?? ""}`}
    >
      <svg
        viewBox="0 0 24 24"
        aria-hidden="true"
        className="h-4 w-4 fill-current"
      >
        <path d="M12 .5a12 12 0 0 0-3.79 23.39c.6.11.82-.26.82-.58v-2.05c-3.34.73-4.04-1.41-4.04-1.41-.55-1.38-1.33-1.74-1.33-1.74-1.09-.74.08-.72.08-.72 1.2.08 1.84 1.22 1.84 1.22 1.08 1.82 2.82 1.29 3.5.99.11-.77.42-1.29.76-1.59-2.67-.3-5.48-1.31-5.48-5.86 0-1.29.47-2.34 1.22-3.16-.12-.3-.53-1.52.12-3.16 0 0 1-.31 3.3 1.21A11.6 11.6 0 0 1 12 6.58c1.02 0 2.05.14 3.01.41 2.3-1.52 3.3-1.21 3.3-1.21.66 1.64.24 2.86.12 3.16.76.82 1.22 1.87 1.22 3.16 0 4.56-2.81 5.55-5.49 5.84.43.37.82 1.1.82 2.23v3.3c0 .32.22.7.83.58A12 12 0 0 0 12 .5Z" />
      </svg>
    </a>
  );
}
