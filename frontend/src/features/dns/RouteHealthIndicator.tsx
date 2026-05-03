import type { ContainerStatus } from "../jobs/types";

type RouteHealthIndicatorProps = {
  routeStatus?: string | null;
  containerStatus?: ContainerStatus | null;
  hasHostname: boolean;
};

export function RouteHealthIndicator({ routeStatus, containerStatus, hasHostname }: RouteHealthIndicatorProps) {
  const dnsReady = routeStatus === "reverse-proxy-configured" && hasHostname;
  const httpReady = Boolean(routeStatus && routeStatus !== "failed" && hasHostname);
  const containerReady = containerStatus === "running";

  return (
    <div className="flex items-center gap-1.5" aria-label="Route health">
      <HealthDot label="DNS" ready={dnsReady} />
      <HealthDot label="HTTP" ready={httpReady} />
      <HealthDot label="Container" ready={containerReady} />
    </div>
  );
}

function HealthDot({ label, ready }: { label: string; ready: boolean }) {
  return (
    <span className={`h-2.5 w-2.5 rounded-full ${ready ? "bg-emerald-500" : "bg-slate-300"}`} title={label} aria-label={label} />
  );
}
