import { api } from "./client";

export type AiInsights = {
  generating?: boolean;
  summary?: string;
  topThemes?: string[];
  sentimentSummary?: string;
  recommendations?: string[];
  [key: string]: unknown;
};

export const aiApi = {
  insights(workspaceId: string) {
    return api<AiInsights | { generating: true }>(`/api/ai/insights?workspaceId=${workspaceId}`);
  },

  replySuggestion(testimonialId: string, workspaceId: string) {
    return api<{ suggestion: string } | { generating: true }>(
      `/api/ai/reply-suggestion/${testimonialId}?workspaceId=${workspaceId}`
    );
  },
};
