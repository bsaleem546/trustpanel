import { api } from "./client";

export const emailApi = {
  sendRequest(payload: {
    workspaceId: string;
    recipientName: string;
    recipientEmail: string;
    formId?: string;
    customMessage?: string;
  }) {
    return api<{}>("/api/email/request", { method: "POST", body: payload });
  },
};
