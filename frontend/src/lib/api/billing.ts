import { api } from "./client";

export type WorkspacePlan = {
  planName: string;
  planCode: string;
  monthlyPrice: number;
  status: "Active" | "Trialing" | "PastDue" | "Canceled" | null;
  currentPeriodEnd: string | null;
  cancelAtPeriodEnd: boolean;
  isTrialing: boolean;
  testimonialLimit: number;
  widgetLimit: number;
  hasVideoTestimonials: boolean;
  hasAiFeatures: boolean;
  hasApiAccess: boolean;
  hasWhiteLabel: boolean;
  hasCustomDomain: boolean;
  hasTeamMembers: boolean;
};

export const billingApi = {
  plan(workspaceId: string) {
    return api<WorkspacePlan>(`/api/billing/plan?workspaceId=${workspaceId}`);
  },

  checkout(priceId: string) {
    return api<{ url: string }>("/api/billing/checkout", {
      method: "POST",
      body: {
        priceId,
        successUrl: `${window.location.origin}/dashboard/settings/billing?checkout=success`,
        cancelUrl: `${window.location.origin}/dashboard/settings/billing`,
      },
    });
  },

  portal() {
    return api<{ url: string }>("/api/billing/portal", {
      method: "POST",
      body: { returnUrl: `${window.location.origin}/dashboard/settings/billing` },
    });
  },
};
