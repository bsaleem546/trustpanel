import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";

import { isAuthenticated } from "./api/client";
import { authApi } from "./api/auth";
import { workspacesApi } from "./api/workspaces";

// localStorage is client-only, so during SSR (and the hydration render) we
// must assume "unknown" and resolve auth state in an effect.
function useHasToken() {
  const [hasToken, setHasToken] = useState<boolean | null>(null);
  useEffect(() => {
    setHasToken(isAuthenticated());
  }, []);
  return hasToken;
}

export function useMe() {
  const hasToken = useHasToken();
  return useQuery({
    queryKey: ["me"],
    queryFn: () => authApi.me(),
    enabled: hasToken === true,
    staleTime: 60_000,
    retry: false,
  });
}

export function useCurrentWorkspace() {
  const { data: me } = useMe();
  return useQuery({
    queryKey: ["workspace", me?.workspaceId],
    queryFn: () => workspacesApi.get(me!.workspaceId!),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });
}

/** Redirects to /login when there is no access token. Client-side only. */
export function useRequireAuth() {
  const hasToken = useHasToken();
  const navigate = useNavigate();
  useEffect(() => {
    if (hasToken === false) {
      navigate({ to: "/login" });
    }
  }, [hasToken, navigate]);
}

export function useLogout() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  return async () => {
    await authApi.logout().catch(() => {});
    queryClient.clear();
    navigate({ to: "/login" });
  };
}
