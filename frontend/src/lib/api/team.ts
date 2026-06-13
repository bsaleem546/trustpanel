import { api } from "./client";

export type WorkspaceRole = "Owner" | "Admin" | "Member" | "Viewer";

export type TeamMember = {
  id: string;
  userId: string | null;
  email: string | null;
  role: WorkspaceRole;
  accepted: boolean;
  createdAt: string;
};

export const teamApi = {
  list(workspaceId: string) {
    return api<TeamMember[]>(`/api/team/?workspaceId=${workspaceId}`);
  },

  invite(workspaceId: string, email: string, role: WorkspaceRole) {
    return api<{ token: string }>("/api/team/invite", {
      method: "POST",
      body: { workspaceId, email, role },
    });
  },

  changeRole(memberId: string, workspaceId: string, role: WorkspaceRole) {
    return api(`/api/team/${memberId}/role`, {
      method: "PUT",
      body: { workspaceId, role },
    });
  },

  remove(memberId: string, workspaceId: string) {
    return api(`/api/team/${memberId}?workspaceId=${workspaceId}`, { method: "DELETE" });
  },
};
