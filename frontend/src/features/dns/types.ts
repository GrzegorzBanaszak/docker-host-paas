import type { ContainerStatus } from "../jobs/types";

export type DnsRoutingSettings = {
  mode: "Port" | "TunnelWildcard" | string;
  publicScheme: string;
  baseDomain?: string | null;
  dockerNetwork: string;
  reverseProxy: string;
};

export type DnsSecretStatus = {
  name: string;
  status: "configured" | "not_configured" | "unknown" | string;
};

export type DnsTunnelStatus = {
  status: "configured" | "not_configured" | "not_used" | "unknown" | string;
  tunnelId?: string | null;
  tunnelHostname?: string | null;
  serviceTarget: string;
  tunnelToken: DnsSecretStatus;
  apiToken: DnsSecretStatus;
};

export type DnsRecord = {
  type: string;
  name: string;
  target: string;
  proxyStatus: string;
  status: string;
};

export type DnsRoute = {
  jobId: string;
  jobName: string;
  repositoryUrl: string;
  generatedImageTag?: string | null;
  containerName?: string | null;
  containerStatus?: ContainerStatus | null;
  containerPort?: number | null;
  publishedPort?: number | null;
  publicAccessEnabled: boolean;
  publicHostname?: string | null;
  routeStatus?: string | null;
  deploymentUrl?: string | null;
  deployedAtUtc?: string | null;
  createdAtUtc: string;
};

export type DnsOverview = {
  routing: DnsRoutingSettings;
  tunnel: DnsTunnelStatus;
  expectedWildcardRecord?: DnsRecord | null;
  configuredRecords: DnsRecord[];
  publicHostnameCount: number;
  runningContainerCount: number;
  routeErrorCount: number;
  lastUpdatedUtc?: string | null;
};
