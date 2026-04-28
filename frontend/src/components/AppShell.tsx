import { Link, NavLink, useLocation } from "react-router-dom";
import type { PropsWithChildren } from "react";
import { Icon } from "./Icon";

export function AppShell({ children }: PropsWithChildren) {
  const location = useLocation();
  const hideSideNav = location.pathname.startsWith("/jobs/") || location.pathname.startsWith("/images/");

  return (
    <div className="blueprint-radial relative min-h-screen text-ink">
      <header className="fixed inset-x-0 top-0 z-50 flex h-14 items-center justify-between border-b border-slate-200 bg-white px-6">
        <div className="flex items-center gap-6">
          <Link to="/" className="text-lg font-black uppercase tracking-[-0.08em] text-slate-900">
            Dockerizer
          </Link>
          <div className="relative hidden md:block">
            <Icon name="search" className="absolute left-2 top-1/2 -translate-y-1/2 text-[18px] text-slate-400" />
            <input
              className="w-56 rounded border border-outline bg-slate-50 py-1 pl-8 pr-3 text-xs outline-none transition focus:border-sky"
              placeholder="Search resources..."
              type="text"
            />
          </div>
        </div>
        <div className="flex items-center gap-4 text-slate-500">
          <button className="rounded p-1.5 transition hover:bg-slate-50 hover:text-slate-700" type="button">
            <Icon name="notifications" className="text-[20px]" />
          </button>
          <button className="rounded p-1.5 transition hover:bg-slate-50 hover:text-slate-700" type="button">
            <Icon name="settings" className="text-[20px]" />
          </button>
          <button className="rounded p-1.5 transition hover:bg-slate-50 hover:text-slate-700" type="button">
            <Icon name="help" className="text-[20px]" />
          </button>
          <div className="flex h-8 w-8 items-center justify-center overflow-hidden rounded border border-slate-300 bg-slate-100 text-slate-500">
            <Icon name="person" className="text-[20px]" />
          </div>
        </div>
      </header>

      {!hideSideNav ? (
        <aside className="fixed bottom-0 left-0 top-14 z-40 hidden w-64 flex-col border-r border-slate-200 bg-slate-50/70 p-4 backdrop-blur md:flex">
          <div className="mb-6 px-2 pt-1">
            <h2 className="text-sm font-bold uppercase tracking-[0.16em] text-slate-900">Control Room</h2>
            <p className="mt-1 font-mono text-[11px] text-slate-500">Engine v2.4.0</p>
          </div>
          <Link
            to="/jobs/new"
            className="mb-4 flex items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-800"
          >
            <Icon name="add" className="text-[18px]" />
            New Job
          </Link>
          <nav className="flex flex-1 flex-col gap-1 text-[13px] font-semibold">
            <ShellLink to="/" label="Dashboard" icon="dashboard" active={location.pathname === "/"} />
            <ShellLink to="/jobs" label="Jobs" icon="terminal" active={location.pathname.startsWith("/jobs")} />
            <ShellLink to="/images" label="Images" icon="layers" active={location.pathname.startsWith("/images")} />
            <ShellGhost label="Clusters" icon="account_tree" />
            <ShellGhost label="Registry" icon="database" />
          </nav>
          <div className="mt-auto border-t border-slate-200 pt-4">
            <ShellGhost label="Docs" icon="menu_book" />
            <ShellGhost label="Support" icon="support_agent" />
          </div>
        </aside>
      ) : null}

      <main className={`relative z-10 px-6 pb-8 pt-[88px] ${hideSideNav ? "mx-auto max-w-7xl" : "md:ml-64"}`}>{children}</main>
    </div>
  );
}

function ShellLink({ to, label, icon, active }: { to: string; label: string; icon: string; active: boolean }) {
  return (
    <NavLink
      to={to}
      className={`flex items-center gap-3 rounded-sm px-4 py-2 transition ${
        active
          ? "border border-slate-200 bg-white text-slate-900 shadow-sm"
          : "text-slate-500 hover:bg-slate-100 hover:text-slate-900"
      }`}
    >
      <Icon name={icon} className="text-[18px]" filled={active} />
      <span>{label}</span>
    </NavLink>
  );
}

function ShellGhost({ label, icon }: { label: string; icon: string }) {
  return (
    <button type="button" className="flex items-center gap-3 rounded-sm px-4 py-2 text-[13px] font-semibold text-slate-500 transition hover:bg-slate-100 hover:text-slate-900">
      <Icon name={icon} className="text-[18px]" />
      <span>{label}</span>
    </button>
  );
}
