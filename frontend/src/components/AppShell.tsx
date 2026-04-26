import { Link, NavLink } from "react-router-dom";
import type { PropsWithChildren } from "react";

export function AppShell({ children }: PropsWithChildren) {
  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(14,165,233,0.15),_transparent_30%),linear-gradient(180deg,_#f8fafc_0%,_#eef2f7_100%)] text-ink">
      <header className="border-b border-slate-200/70 bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
          <Link to="/" className="text-xl font-semibold tracking-tight text-ink">
            Dockerizer
          </Link>
          <nav className="flex gap-2 text-sm font-medium">
            <NavLink
              to="/"
              className={({ isActive }) =>
                `rounded-full px-4 py-2 transition ${isActive ? "bg-ink text-white" : "text-steel hover:bg-slate-100"}`
              }
            >
              Dashboard
            </NavLink>
            <NavLink
              to="/jobs"
              className={({ isActive }) =>
                `rounded-full px-4 py-2 transition ${isActive ? "bg-ink text-white" : "text-steel hover:bg-slate-100"}`
              }
            >
              Jobs
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-7xl px-6 py-8">{children}</main>
    </div>
  );
}
