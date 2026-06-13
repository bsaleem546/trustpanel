import { api } from "./client";

export type SubmissionType = "Text" | "Video" | "Both";

export type Form = {
  id: string;
  name: string;
  slug: string;
  allowedSubmissionType: SubmissionType;
  isActive: boolean;
  createdAt: string;
  workspaceId: string;
};

export const formsApi = {
  list(workspaceId?: string) {
    const qs = workspaceId ? `?workspaceId=${workspaceId}` : "";
    return api<{ items: Form[]; total: number }>(`/api/forms/${qs}`);
  },

  get(formId: string) {
    return api<Form>(`/api/forms/${formId}`);
  },

  create(payload: { name: string; allowedSubmissionType?: SubmissionType; isActive?: boolean }) {
    return api<Form>("/api/forms/", { method: "POST", body: payload });
  },

  update(formId: string, payload: { name?: string; allowedSubmissionType?: SubmissionType; isActive?: boolean }) {
    return api<Form>(`/api/forms/${formId}`, { method: "PUT", body: payload });
  },

  remove(formId: string) {
    return api(`/api/forms/${formId}`, { method: "DELETE" });
  },
};
