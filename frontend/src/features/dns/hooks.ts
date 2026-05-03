import { useQuery } from "@tanstack/react-query";
import { api } from "../../lib/api";

export function useDnsOverview() {
  return useQuery({
    queryKey: ["dns", "overview"],
    queryFn: api.getDnsOverview,
    refetchInterval: 5000
  });
}

export function useDnsRoutes() {
  return useQuery({
    queryKey: ["dns", "routes"],
    queryFn: api.getDnsRoutes,
    refetchInterval: 5000
  });
}
