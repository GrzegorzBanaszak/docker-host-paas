import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { EmptyState } from "../components/EmptyState";
import { Icon } from "../components/Icon";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { DnsRecordPreview } from "../features/dns/DnsRecordPreview";
import { HostnameLink } from "../features/dns/HostnameLink";
import { useDnsOverview, useDnsRoutes } from "../features/dns/hooks";
import { RouteHealthIndicator } from "../features/dns/RouteHealthIndicator";
import { RouteStatusBadge } from "../features/dns/RouteStatusBadge";
import { SecretStatus } from "../features/dns/SecretStatus";
import type { DnsOverview as DnsOverviewModel, DnsRecord, DnsRoute } from "../features/dns/types";
import { usePublishDnsRoute, useRestartContainer, useUnpublishDnsRoute } from "../features/jobs/hooks";

type DnsTab = "overview" | "routes" | "settings";
type ContainerFilter = "all" | "running" | "stopped" | "not_found";
type RouteFilter = "all" | "configured" | "failed" | "pending";

export function DnsPage() {
  const overviewQuery = useDnsOverview();
  const routesQuery = useDnsRoutes();
  const [activeTab, setActiveTab] = useState<DnsTab>("overview");
  const [containerFilter, setContainerFilter] = useState<ContainerFilter>("all");
  const [routeFilter, setRouteFilter] = useState<RouteFilter>("all");
  const [search, setSearch] = useState("");

  const overview = overviewQuery.data;
  const routes = routesQuery.data ?? [];
  const filteredRoutes = useMemo(() => {
    const normalizedSearch = search.trim().toLowerCase();

    return routes
      .filter((route) => matchesContainerFilter(route, containerFilter))
      .filter((route) => matchesRouteFilter(route, routeFilter))
      .filter((route) => {
        if (!normalizedSearch) {
          return true;
        }

        const haystack = [
          route.publicHostname ?? "",
          route.deploymentUrl ?? "",
          route.jobName,
          route.repositoryUrl,
          route.containerName ?? "",
          route.generatedImageTag ?? ""
        ]
          .join(" ")
          .toLowerCase();

        return haystack.includes(normalizedSearch);
      });
  }, [containerFilter, routes, routeFilter, search]);

  async function handleRefresh() {
    await Promise.all([overviewQuery.refetch(), routesQuery.refetch()]);
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Networking"
        title="DNS"
        description="Public routing status for deployments published through Dockerizer."
      />

      <div className="flex flex-col gap-3 rounded border border-outline bg-white p-3 md:flex-row md:items-center md:justify-between">
        <div className="flex flex-wrap gap-2">
          <TabButton active={activeTab === "overview"} label="Overview" onClick={() => setActiveTab("overview")} />
          <TabButton active={activeTab === "routes"} label="Routes" onClick={() => setActiveTab("routes")} />
          <TabButton active={activeTab === "settings"} label="Settings" onClick={() => setActiveTab("settings")} />
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            type="button"
            onClick={() => void handleRefresh()}
            className="inline-flex h-9 items-center justify-center gap-2 rounded border border-outline bg-white px-3 text-[11px] font-bold uppercase tracking-[0.12em] text-steel transition hover:bg-slate-50 hover:text-ink"
          >
            <Icon name="refresh" className={`text-[16px] ${overviewQuery.isFetching || routesQuery.isFetching ? "animate-spin" : ""}`} />
            Refresh
          </button>
          <a
            href="https://dash.cloudflare.com"
            target="_blank"
            rel="noreferrer"
            className="inline-flex h-9 items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-3 text-[11px] font-bold uppercase tracking-[0.12em] text-white transition hover:bg-slate-800"
          >
            <Icon name="open_in_new" className="text-[16px]" />
            Open Cloudflare
          </a>
        </div>
      </div>

      {overviewQuery.isError ? <StatusMessage tone="error" title="DNS API unavailable" message={overviewQuery.error.message} /> : null}
      {routesQuery.isError ? <StatusMessage tone="error" title="DNS routes unavailable" message={routesQuery.error.message} /> : null}

      {!overview ? (
        <StatusMessage tone="info" title="Loading DNS" message="Fetching current routing configuration from the backend." />
      ) : null}

      {overview && activeTab === "overview" ? <DnsOverview overview={overview} /> : null}

      {overview && activeTab === "routes" ? (
        <DnsRoutes
          routes={filteredRoutes}
          routeCount={routes.length}
          containerFilter={containerFilter}
          routeFilter={routeFilter}
          search={search}
          overview={overview}
          onContainerFilterChange={setContainerFilter}
          onRouteFilterChange={setRouteFilter}
          onSearchChange={setSearch}
        />
      ) : null}

      {overview && activeTab === "settings" ? <DnsSettings overview={overview} /> : null}
    </div>
  );
}

function DnsOverview({ overview }: { overview: DnsOverviewModel }) {
  return (
    <div className="space-y-4">
      {overview.routing.mode === "Port" ? (
        <StatusMessage tone="warning" title="DNS routing inactive" message="Current configuration uses port-published URLs. Wildcard DNS becomes active after ApplicationRouting:Mode is set to TunnelWildcard." />
      ) : null}

      <section className="grid gap-4 xl:grid-cols-4">
        <StatCard label="Public Hostnames" value={overview.publicHostnameCount} icon="public" />
        <StatCard label="Running Containers" value={overview.runningContainerCount} icon="check_circle" tone="text-emerald-600" />
        <StatCard label="Route Errors" value={overview.routeErrorCount} icon="error" tone="text-rose-700" />
        <StatCard label="Last Update" value={overview.lastUpdatedUtc ? formatDate(overview.lastUpdatedUtc) : "-"} icon="schedule" compact />
      </section>

      <Panel title="DNS Record" description="Wildcard DNS target and container hostnames currently configured by Dockerizer.">
        <div className="space-y-3">
          {overview.expectedWildcardRecord ? <DnsRecordRow record={overview.expectedWildcardRecord} /> : (
            <StatusMessage tone="info" title="No wildcard record" message="Backend does not report an active wildcard DNS record for the current routing mode." />
          )}

          {overview.configuredRecords.length > 0 ? (
            <div className="space-y-2">
              <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-steel">Container records</p>
              {overview.configuredRecords.map((record) => (
                <DnsRecordRow key={`${record.type}:${record.name}`} record={record} />
              ))}
            </div>
          ) : (
            <EmptyState title="No public container records" description="Publish a route to create the first public hostname." />
          )}

          <div className="flex flex-wrap gap-2">
            <CopyButton
              label="Copy wildcard DNS"
              value={formatDnsRecord(overview.expectedWildcardRecord)}
              disabled={!overview.expectedWildcardRecord}
            />
            <CopyButton
              label="Copy tunnel hostname"
              value={overview.tunnel.tunnelHostname ?? ""}
              disabled={!overview.tunnel.tunnelHostname}
            />
          </div>
        </div>
      </Panel>
    </div>
  );
}

function DnsRecordRow({ record }: { record: DnsRecord }) {
  return (
    <div className="space-y-2">
      <DnsRecordPreview type={record.type} name={record.name} target={record.target} proxyStatus={record.proxyStatus} />
      <div className="flex items-center gap-2 text-[11px] font-bold uppercase tracking-[0.08em] text-steel">
        <span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" />
        {record.status.replace(/_/g, " ")}
      </div>
    </div>
  );
}

function DnsRoutes({
  routes,
  routeCount,
  containerFilter,
  routeFilter,
  search,
  overview,
  onContainerFilterChange,
  onRouteFilterChange,
  onSearchChange
}: {
  routes: DnsRoute[];
  routeCount: number;
  containerFilter: ContainerFilter;
  routeFilter: RouteFilter;
  search: string;
  overview: DnsOverviewModel;
  onContainerFilterChange: (value: ContainerFilter) => void;
  onRouteFilterChange: (value: RouteFilter) => void;
  onSearchChange: (value: string) => void;
}) {
  const [routeToPublish, setRouteToPublish] = useState<DnsRoute | null>(null);

  return (
    <div className="space-y-4">
      {overview.routing.mode === "Port" ? (
        <StatusMessage tone="info" title="Port routing mode" message="DNS routes are not active in Port mode. Publishing requires TunnelWildcard routing in backend configuration." />
      ) : null}

      <div className="rounded border border-outline bg-white p-3">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex flex-wrap items-center gap-2">
            <span className="mr-1 text-sm font-medium text-steel">Container:</span>
            {(["all", "running", "stopped", "not_found"] as ContainerFilter[]).map((value) => (
              <FilterButton key={value} active={containerFilter === value} label={value.replace("_", " ")} onClick={() => onContainerFilterChange(value)} />
            ))}
            <span className="mx-1 hidden h-6 border-l border-outline sm:block" />
            <span className="mr-1 text-sm font-medium text-steel">Route:</span>
            {(["all", "configured", "failed", "pending"] as RouteFilter[]).map((value) => (
              <FilterButton key={value} active={routeFilter === value} label={value} onClick={() => onRouteFilterChange(value)} />
            ))}
          </div>
          <div className="relative w-full xl:w-80">
            <Icon name="search" className="absolute left-2 top-1/2 -translate-y-1/2 text-[16px] text-slate-400" />
            <input
              className="h-9 w-full rounded border border-outline bg-white pl-8 pr-3 text-sm outline-none focus:border-sky"
              placeholder="Search hostname, job, repo, container..."
              type="text"
              value={search}
              onChange={(event) => onSearchChange(event.target.value)}
            />
          </div>
        </div>
      </div>

      <Panel title="Routes" description={`${routeCount} applications can be published through Dockerizer DNS routes.`}>
        {routes.length === 0 ? (
          <EmptyState title="No routes found" description="Create or deploy a job to publish the first route." />
        ) : (
          <div className="overflow-hidden rounded border border-outline">
            <table className="min-w-full border-collapse bg-white">
              <thead>
                <tr className="border-b border-outline bg-slate-50 text-left text-[11px] font-bold uppercase tracking-[0.12em] text-steel">
                  <th className="px-4 py-3">Hostname</th>
                  <th className="px-4 py-3">Job</th>
                  <th className="px-4 py-3">Container</th>
                  <th className="px-4 py-3">Route</th>
                  <th className="px-4 py-3">Image</th>
                  <th className="px-4 py-3">Health</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="text-sm">
                {routes.map((route) => (
                  <DnsRouteRow
                    key={route.jobId}
                    route={route}
                    canPublishDns={overview.routing.mode === "TunnelWildcard" && Boolean(overview.routing.baseDomain)}
                    onPublish={() => setRouteToPublish(route)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>

      {routeToPublish ? (
        <PublishRouteModal
          route={routeToPublish}
          overview={overview}
          onClose={() => setRouteToPublish(null)}
        />
      ) : null}
    </div>
  );
}

function DnsRouteRow({ route, canPublishDns, onPublish }: { route: DnsRoute; canPublishDns: boolean; onPublish: () => void }) {
  const restartContainer = useRestartContainer(route.jobId);
  const unpublishRoute = useUnpublishDnsRoute(route.jobId);
  const copyValue = route.deploymentUrl ?? route.publicHostname ?? "";
  const canRestart = Boolean(route.containerStatus && route.containerStatus !== "not_found");
  const canPublish = Boolean(route.generatedImageTag && route.containerPort && canPublishDns);
  const routeMutationPending = unpublishRoute.isPending;

  return (
    <tr className="border-b border-outline last:border-b-0 hover:bg-[rgba(211,228,254,0.3)]">
      <td className="max-w-[260px] px-4 py-3">
        <HostnameLink hostname={route.publicHostname ?? (route.publishedPort ? `:${route.publishedPort}` : null)} url={route.deploymentUrl} />
      </td>
      <td className="px-4 py-3">
        <div className="min-w-0">
          <Link className="font-semibold text-secondary underline-offset-2 hover:underline" to={`/jobs/${route.jobId}`}>
            {route.jobName}
          </Link>
          <p className="mt-1 font-mono text-[11px] text-steel">#{route.jobId.slice(0, 8)}</p>
        </div>
      </td>
      <td className="px-4 py-3">
        <div className="space-y-1">
          <ContainerStatusChip status={route.containerStatus} />
          <p className="font-mono text-[11px] text-steel">{route.containerName ?? "name unavailable"}</p>
          <p className="font-mono text-[11px] text-steel">{formatPortMapping(route)}</p>
        </div>
      </td>
      <td className="px-4 py-3">
        <div className="space-y-1.5">
          <RouteStatusBadge status={route.routeStatus} />
          <p className="font-mono text-[11px] text-steel">{formatDate(route.deployedAtUtc ?? route.createdAtUtc)}</p>
        </div>
      </td>
      <td className="max-w-[220px] px-4 py-3">
        <p className="truncate font-mono text-[11px] text-ink" title={route.generatedImageTag ?? undefined}>
          {route.generatedImageTag ?? "-"}
        </p>
      </td>
      <td className="px-4 py-3">
        <RouteHealthIndicator routeStatus={route.routeStatus} containerStatus={route.containerStatus} hasHostname={Boolean(route.publicHostname)} />
      </td>
      <td className="px-4 py-3 text-right">
        <div className="flex items-center justify-end gap-2">
          {route.deploymentUrl ? (
            <a
              className="inline-flex h-8 w-8 items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink"
              href={route.deploymentUrl}
              target="_blank"
              rel="noreferrer"
              title="Open"
              aria-label="Open"
            >
              <Icon name="open_in_new" className="text-[16px]" />
            </a>
          ) : null}
          <CopyIconButton value={copyValue} disabled={!copyValue} />
          {route.publicAccessEnabled ? (
            <button
              type="button"
              disabled={routeMutationPending}
              onClick={() => unpublishRoute.mutate()}
              className="inline-flex h-8 items-center justify-center gap-1.5 rounded-sm border border-outline bg-white px-2 text-[10px] font-bold uppercase tracking-[0.08em] text-steel transition hover:bg-slate-50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-50"
              title="Remove public access"
            >
              <Icon name={routeMutationPending ? "progress_activity" : "lock"} className={`text-[15px] ${routeMutationPending ? "animate-spin" : ""}`} />
              Private
            </button>
          ) : (
            <button
              type="button"
              disabled={!canPublish}
              onClick={onPublish}
              className="inline-flex h-8 items-center justify-center gap-1.5 rounded-sm border border-slate-900 bg-slate-900 px-2 text-[10px] font-bold uppercase tracking-[0.08em] text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:border-outline disabled:bg-slate-100 disabled:text-slate-400"
              title={canPublishDns ? (canPublish ? "Preview public URL" : "Build and deploy this job before publishing DNS") : "Enable TunnelWildcard routing in backend configuration"}
            >
              <Icon name="public" className="text-[15px]" />
              Publish
            </button>
          )}
          <button
            type="button"
            disabled={!canRestart || restartContainer.isPending}
            onClick={() => restartContainer.mutate()}
            className="inline-flex h-8 w-8 items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-50"
            title="Restart container"
            aria-label="Restart container"
          >
            <Icon name={restartContainer.isPending ? "progress_activity" : "restart_alt"} className={`text-[16px] ${restartContainer.isPending ? "animate-spin" : ""}`} />
          </button>
          <Link
            className="inline-flex h-8 w-8 items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink"
            to={`/jobs/${route.jobId}`}
            title="Job details"
            aria-label="Job details"
          >
            <Icon name="arrow_forward" className="text-[16px]" />
          </Link>
        </div>
      </td>
    </tr>
  );
}

function PublishRouteModal({ route, overview, onClose }: { route: DnsRoute; overview: DnsOverviewModel; onClose: () => void }) {
  const publishRoute = usePublishDnsRoute(route.jobId);
  const previewHostname = route.publicHostname ?? buildPublicHostnamePreview(route, overview.routing.baseDomain);
  const previewUrl = previewHostname ? `${overview.routing.publicScheme}://${previewHostname}` : "generated by backend";

  function handlePublish() {
    publishRoute.mutate(undefined, {
      onSuccess: onClose
    });
  }

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/40 px-4 py-6">
      <div className="w-full max-w-lg rounded border border-outline bg-white shadow-xl">
        <div className="flex items-start justify-between gap-4 border-b border-outline p-5">
          <div>
            <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-steel">Publish route</p>
            <h2 className="mt-1 text-xl font-bold text-ink">{route.jobName}</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="inline-flex h-8 w-8 items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink"
            aria-label="Close"
          >
            <Icon name="close" className="text-[18px]" />
          </button>
        </div>

        <div className="space-y-4 p-5">
          <div className="rounded border border-outline bg-surface p-4">
            <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">Public URL</p>
            <p className="mt-2 break-all font-mono text-sm font-semibold text-secondary">{previewUrl}</p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <CompactFact label="App port" value={route.containerPort ? route.containerPort.toString() : "unavailable"} />
            <CompactFact label="Access after publish" value="Public" />
          </div>

          <p className="rounded border border-sky-200 bg-sky-50 px-3 py-2 text-sm text-sky-800">
            Dockerizer will recreate the container with reverse proxy routing. The Open action will use this public URL after publishing.
          </p>
        </div>

        <div className="border-t border-outline bg-slate-50 p-5">
          {publishRoute.isError ? (
            <p className="rounded border border-rose-300 bg-rose-50 px-3 py-2 text-sm text-rose-800">{publishRoute.error.message}</p>
          ) : null}
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="inline-flex h-9 items-center justify-center rounded border border-outline bg-white px-3 text-sm font-medium text-ink transition hover:bg-slate-50"
            >
              Cancel
            </button>
            <button
              type="button"
              disabled={publishRoute.isPending}
              onClick={handlePublish}
              className="inline-flex h-9 items-center justify-center gap-2 rounded border border-slate-900 bg-slate-900 px-3 text-sm font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Icon name={publishRoute.isPending ? "progress_activity" : "public"} className={`text-[16px] ${publishRoute.isPending ? "animate-spin" : ""}`} />
              Publish
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function CompactFact({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-outline bg-white px-3 py-2">
      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
      <p className="mt-1 font-mono text-[12px] font-semibold text-ink">{value}</p>
    </div>
  );
}

function DnsSettings({ overview }: { overview: DnsOverviewModel }) {
  const appsettingsSnippet = JSON.stringify(
    {
      ApplicationRouting: {
        Mode: "TunnelWildcard",
        PublicScheme: overview.routing.publicScheme,
        BaseDomain: overview.routing.baseDomain ?? "apps.example.com",
        DockerNetwork: overview.routing.dockerNetwork,
        ReverseProxy: overview.routing.reverseProxy
      }
    },
    null,
    2
  );
  const dnsRecord = formatDnsRecord(overview.expectedWildcardRecord);
  const composeCommand = "docker compose --profile tunnel up -d traefik cloudflared";

  return (
    <div className="grid gap-4 xl:grid-cols-2">
      <Panel title="ApplicationRouting" description="Read-only configuration reported by the backend.">
        <DefinitionGrid
          items={[
            ["Routing mode", overview.routing.mode],
            ["Public scheme", overview.routing.publicScheme],
            ["Base domain", overview.routing.baseDomain ?? "not configured"],
            ["Docker network", overview.routing.dockerNetwork],
            ["Reverse proxy", overview.routing.reverseProxy],
            ["DNS provider status", overview.tunnel.apiToken.status]
          ]}
        />
      </Panel>

      <Panel title="Cloudflare Tunnel" description="Tunnel values reported from backend configuration without exposing secrets.">
        <DefinitionGrid
          items={[
            ["Tunnel status", overview.tunnel.status],
            ["Tunnel id", overview.tunnel.tunnelId ?? "not configured"],
            ["Tunnel hostname", overview.tunnel.tunnelHostname ?? "not configured"],
            ["Tunnel service", overview.tunnel.serviceTarget]
          ]}
        />
      </Panel>

      <Panel title="Secrets" description="Secret values are never rendered by this frontend view.">
        <div className="space-y-2">
          <SecretStatus label={overview.tunnel.tunnelToken.name} status={overview.tunnel.tunnelToken.status} />
          <SecretStatus label={overview.tunnel.apiToken.name} status={overview.tunnel.apiToken.status} />
        </div>
      </Panel>

      <Panel title="Configuration Snippets" description="Copy helpers for the wildcard tunnel setup.">
        <div className="space-y-3">
          <pre className="overflow-auto rounded border border-outline bg-slate-950 p-3 text-[11px] leading-5 text-slate-100">{appsettingsSnippet}</pre>
          <div className="flex flex-wrap gap-2">
            <CopyButton label="Copy appsettings snippet" value={appsettingsSnippet} />
            <CopyButton label="Copy DNS record" value={dnsRecord} disabled={!dnsRecord} />
            <CopyButton label="Copy cloudflared compose command" value={composeCommand} />
          </div>
        </div>
      </Panel>
    </div>
  );
}

function TabButton({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`h-9 rounded border px-3 text-[11px] font-bold uppercase tracking-[0.12em] transition ${
        active ? "border-slate-900 bg-slate-900 text-white" : "border-outline bg-white text-steel hover:bg-slate-50 hover:text-ink"
      }`}
    >
      {label}
    </button>
  );
}

function FilterButton({ active, label, onClick }: { active: boolean; label: string; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded border px-3 py-1 text-sm capitalize transition ${
        active ? "border-outline bg-variant text-ink" : "border-outline bg-white text-steel hover:bg-slate-50"
      }`}
    >
      {label}
    </button>
  );
}

function CopyButton({ label, value, disabled = false }: { label: string; value: string; disabled?: boolean }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    if (disabled || !value || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(value);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <button
      type="button"
      disabled={disabled}
      onClick={() => void handleCopy()}
      className="inline-flex h-9 items-center justify-center gap-2 rounded border border-outline bg-white px-3 text-[11px] font-bold uppercase tracking-[0.12em] text-steel transition hover:bg-slate-50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-50"
    >
      <Icon name={copied ? "check" : "content_copy"} className="text-[16px]" />
      {copied ? "Copied" : label}
    </button>
  );
}

function CopyIconButton({ value, disabled }: { value: string; disabled: boolean }) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    if (disabled || !value || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(value);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  }

  return (
    <button
      type="button"
      disabled={disabled}
      onClick={() => void handleCopy()}
      className="inline-flex h-8 w-8 items-center justify-center rounded-sm border border-outline bg-white text-steel transition hover:bg-slate-50 hover:text-ink disabled:cursor-not-allowed disabled:opacity-50"
      title={copied ? "Copied" : "Copy URL"}
      aria-label={copied ? "Copied" : "Copy URL"}
    >
      <Icon name={copied ? "check" : "content_copy"} className="text-[16px]" />
    </button>
  );
}

function DefinitionGrid({ items }: { items: Array<[string, string]> }) {
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {items.map(([label, value]) => (
        <div key={label} className="min-w-0 rounded border border-outline bg-surface px-3 py-2">
          <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
          <p className="mt-1 truncate font-mono text-[12px] font-semibold text-ink" title={value}>
            {value}
          </p>
        </div>
      ))}
    </div>
  );
}

function StatCard({ label, value, icon, tone, compact = false }: { label: string; value: number | string; icon: string; tone?: string; compact?: boolean }) {
  return (
    <div className="relative overflow-hidden rounded border border-outline bg-white p-4">
      <div className="absolute right-0 top-0 h-16 w-16 bg-gradient-to-bl from-variant to-transparent opacity-50" />
      <div className="relative z-10 flex items-start justify-between gap-3">
        <p className="text-[11px] font-bold uppercase tracking-[0.12em] text-steel">{label}</p>
        <Icon name={icon} className={`text-[18px] ${tone ?? "text-slate-400"}`} />
      </div>
      <p className={`relative z-10 mt-3 font-bold text-ink ${compact ? "text-[18px]" : "text-[32px]"}`}>{value}</p>
    </div>
  );
}

function StatusMessage({ tone, title, message }: { tone: "info" | "warning" | "error"; title: string; message: string }) {
  const className =
    tone === "error"
      ? "border-rose-300 bg-rose-50 text-rose-800"
      : tone === "warning"
        ? "border-amber-300 bg-amber-50 text-amber-800"
        : "border-sky-200 bg-sky-50 text-sky-800";

  return (
    <div className={`rounded border p-4 ${className}`}>
      <p className="text-[11px] font-bold uppercase tracking-[0.12em]">{title}</p>
      <p className="mt-1 text-sm">{message}</p>
    </div>
  );
}

function ContainerStatusChip({ status }: { status?: string | null }) {
  const normalized = status ?? "not_found";
  const className =
    normalized === "running"
      ? "border-emerald-200 bg-emerald-50 text-emerald-700"
      : normalized === "restarting"
        ? "border-sky-200 bg-sky-50 text-sky-700"
        : normalized === "paused"
          ? "border-amber-200 bg-amber-50 text-amber-700"
          : normalized === "created"
            ? "border-slate-200 bg-slate-50 text-slate-700"
            : normalized === "exited" || normalized === "dead"
              ? "border-rose-200 bg-rose-50 text-rose-700"
              : "border-outline bg-surface-low text-steel";

  return (
    <span className={`inline-flex items-center gap-1.5 rounded border px-2 py-1 text-[11px] font-bold uppercase tracking-[0.08em] ${className}`}>
      <span className="h-1.5 w-1.5 rounded-full bg-current opacity-80" />
      {normalized.replace("_", " ")}
    </span>
  );
}

function matchesContainerFilter(route: DnsRoute, filter: ContainerFilter) {
  const status = route.containerStatus ?? "not_found";
  if (filter === "all") {
    return true;
  }

  if (filter === "running") {
    return status === "running";
  }

  if (filter === "not_found") {
    return status === "not_found";
  }

  return status !== "running" && status !== "not_found";
}

function matchesRouteFilter(route: DnsRoute, filter: RouteFilter) {
  if (filter === "all") {
    return true;
  }

  if (filter === "configured") {
    return route.publicAccessEnabled && (route.routeStatus === "reverse-proxy-configured" || route.routeStatus === "port-published");
  }

  if (filter === "failed") {
    return route.routeStatus === "failed";
  }

  return !route.publicAccessEnabled || !route.routeStatus || !route.deploymentUrl;
}

function formatPortMapping(route: DnsRoute) {
  if (route.containerPort && route.publishedPort) {
    return `${route.publishedPort} -> ${route.containerPort}`;
  }

  if (route.containerPort) {
    return `proxy -> ${route.containerPort}`;
  }

  if (route.publishedPort) {
    return `:${route.publishedPort}`;
  }

  return "port unavailable";
}

function formatDate(value: string) {
  return new Date(value).toLocaleString();
}

function formatDnsRecord(record?: DnsRecord | null) {
  return record ? `${record.type} ${record.name} ${record.target} ${record.proxyStatus}` : "";
}

function buildPublicHostnamePreview(route: DnsRoute, baseDomain?: string | null) {
  if (!baseDomain) {
    return undefined;
  }

  const source = route.jobName.trim().toLowerCase() || "app";
  let slug = "";
  let lastWasHyphen = false;

  for (const character of source) {
    if ((character >= "a" && character <= "z") || (character >= "0" && character <= "9")) {
      slug += character;
      lastWasHyphen = false;
      continue;
    }

    if (!lastWasHyphen) {
      slug += "-";
      lastWasHyphen = true;
    }
  }

  slug = slug.replace(/^-+|-+$/g, "") || "app";
  if (slug.length > 48) {
    slug = slug.slice(0, 48).replace(/-+$/g, "");
  }

  return `${slug}-${route.jobId.replace(/-/g, "").slice(0, 8)}.${baseDomain}`;
}
