import { api } from "./client";

export type ApiKey = {
  id: string;
  name: string;
  keyPreview: string;
  createdAt: string;
  lastUsedAt: string | null;
  workspaceId: string;
};

export type CreatedApiKey = {
  key: ApiKey;
  plaintextKey: string;
};

export type WebhookEndpoint = {
  id: string;
  url: string;
  workspaceId: string;
  createdAt: string;
};

export const apiKeysApi = {
  list: (workspaceId: string) =>
    api<ApiKey[]>(`/api/apikeys?workspaceId=${workspaceId}`),

  create: (workspaceId: string, name: string) =>
    api<CreatedApiKey>("/api/apikeys", {
      method: "POST",
      body: { workspaceId, name },
    }),

  rename: (keyId: string, workspaceId: string, name: string) =>
    api<void>(`/api/apikeys/${keyId}/rename`, {
      method: "PUT",
      body: { workspaceId, name },
    }),

  revoke: (keyId: string, workspaceId: string) =>
    api<void>(`/api/apikeys/${keyId}?workspaceId=${workspaceId}`, {
      method: "DELETE",
    }),
};

export const webhooksApi = {
  create: (workspaceId: string, url: string) =>
    api<WebhookEndpoint>("/api/webhooks", {
      method: "POST",
      body: { workspaceId, url },
    }),

  remove: (endpointId: string, workspaceId: string) =>
    api<void>(`/api/webhooks/${endpointId}?workspaceId=${workspaceId}`, {
      method: "DELETE",
    }),
};
