import { api } from "./client";

export const billingApi = {
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
