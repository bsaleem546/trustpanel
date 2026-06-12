import { api } from "./client";

export type WorkspaceBranding = {
  logoPath: string | null;
  primaryColor: string;
  secondaryColor: string;
  fontFamily: string;
  showTrustPanelBranding: boolean;
};

export type WorkspaceEmailSender = {
  fromName: string | null;
  fromEmail: string | null;
};

export type Workspace = {
  id: string;
  name: string;
  slug: string;
  customDomain: string | null;
  domainVerifiedAt: string | null;
  branding: WorkspaceBranding;
  emailFrom: WorkspaceEmailSender;
  isOwner: boolean;
  createdAt: string;
};

export type CustomDomain = {
  domain: string;
  cnameTarget: string;
  verified: boolean;
  verifiedAt: string | null;
};

export type BrandingInput = Partial<{
  logoPath: string;
  primaryColor: string;
  secondaryColor: string;
  fontFamily: string;
  showTrustPanelBranding: boolean;
  emailFromName: string;
  emailFromAddress: string;
}>;

export const workspacesApi = {
  list() {
    return api<{ items: Workspace[]; total: number }>("/api/workspaces/");
  },

  create(name: string) {
    return api<Workspace>("/api/workspaces/", { method: "POST", body: { name } });
  },

  get(workspaceId: string) {
    return api<Workspace>(`/api/workspaces/${workspaceId}`);
  },

  update(workspaceId: string, name: string) {
    return api<Workspace>(`/api/workspaces/${workspaceId}`, { method: "PUT", body: { name } });
  },

  remove(workspaceId: string) {
    return api(`/api/workspaces/${workspaceId}`, { method: "DELETE" });
  },

  updateBranding(workspaceId: string, branding: BrandingInput) {
    return api<Workspace>(`/api/workspaces/${workspaceId}/branding`, {
      method: "PUT",
      body: branding,
    });
  },

  setCustomDomain(workspaceId: string, domain: string) {
    return api<CustomDomain>(`/api/workspaces/${workspaceId}/domain`, {
      method: "PUT",
      body: { domain },
    });
  },

  removeCustomDomain(workspaceId: string) {
    return api(`/api/workspaces/${workspaceId}/domain`, { method: "DELETE" });
  },

  verifyCustomDomain(workspaceId: string) {
    return api<CustomDomain>(`/api/workspaces/${workspaceId}/domain/verify`, { method: "POST" });
  },
};
