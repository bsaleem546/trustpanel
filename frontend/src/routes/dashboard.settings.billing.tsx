import { createFileRoute, useSearch } from "@tanstack/react-router";
import { useMutation } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Check, Download, CreditCard } from "lucide-react";
import { billingApi } from "@/lib/api/billing";
import { useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/settings/billing")({
  head: () => ({ meta: [{ title: "Billing — TrustPanel" }] }),
  validateSearch: (s: Record<string, unknown>) => ({ checkout: s.checkout as string | undefined }),
  component: Billing,
});

// Hard-coded price ID — replace with your actual Stripe price ID
const PRO_PRICE_ID = "price_pro_monthly";

function Billing() {
  useRequireAuth();
  const { checkout } = useSearch({ from: "/dashboard/settings/billing" });

  const openCheckout = useMutation({
    mutationFn: (priceId: string) => billingApi.checkout(priceId),
    onSuccess: ({ url }) => { window.location.href = url; },
  });

  const openPortal = useMutation({
    mutationFn: () => billingApi.portal(),
    onSuccess: ({ url }) => { window.location.href = url; },
  });

  const portalBusy = openPortal.isPending;

  return (
    <DashboardLayout title="Billing">
      {checkout === "success" && (
        <div className="tp-card p-4 mb-4 flex items-center gap-2" style={{ background: "rgba(52,211,153,0.08)", borderColor: "var(--success)" }}>
          <Check size={16} style={{ color: "var(--success)" }} />
          <span className="text-sm" style={{ color: "var(--success)" }}>Payment successful — your plan has been upgraded!</span>
        </div>
      )}

      <div className="grid lg:grid-cols-3 gap-6 mb-6">
        <div className="lg:col-span-2 tp-card p-6">
          <div className="flex items-start justify-between mb-4">
            <div>
              <Pill tone="primary">CURRENT PLAN</Pill>
              <div className="mt-3 text-2xl font-semibold">Agency</div>
              <div className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
                $119/mo · renews April 12, 2026
              </div>
            </div>
            <button
              className="tp-btn tp-btn-primary"
              onClick={() => openCheckout.mutate(PRO_PRICE_ID)}
              disabled={openCheckout.isPending}
            >
              {openCheckout.isPending ? "Redirecting…" : "Upgrade plan"}
            </button>
          </div>
          <div className="grid grid-cols-2 gap-y-2 mt-5 text-sm">
            {["10 workspaces", "Unlimited testimonials", "Unlimited widgets", "White-label domain", "AI insights", "Priority support"].map((f) => (
              <div key={f} className="flex items-center gap-2">
                <Check size={14} style={{ color: "var(--success)" }} />
                <span>{f}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="tp-card p-6">
          <div className="font-semibold mb-4">Payment method</div>
          <div className="tp-card p-4 flex items-center gap-3" style={{ background: "var(--surface)" }}>
            <div className="w-10 h-7 rounded flex items-center justify-center" style={{ background: "linear-gradient(135deg,#7c6af7,#60a5fa)" }}>
              <CreditCard size={14} color="white" />
            </div>
            <div className="flex-1">
              <div className="text-sm font-medium">Visa ending 4242</div>
              <div className="text-xs" style={{ color: "var(--subtle)" }}>Expires 09/28</div>
            </div>
          </div>
          <button
            className="tp-btn tp-btn-ghost w-full mt-3"
            onClick={() => openPortal.mutate()}
            disabled={portalBusy}
          >
            {portalBusy ? "Redirecting…" : "Manage payment & invoices"}
          </button>
        </div>
      </div>

      <div className="tp-card p-6 mb-6">
        <div className="font-semibold mb-4">Usage this billing cycle</div>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-5">
          {[
            { l: "Workspaces", v: 2, max: 10 },
            { l: "Testimonials", v: 247, max: null },
            { l: "Widgets", v: 4, max: 10 },
            { l: "Team members", v: 3, max: 10 },
          ].map((u) => (
            <div key={u.l}>
              <div className="flex justify-between text-sm mb-2">
                <span>{u.l}</span>
                <span style={{ color: "var(--subtle)" }}>{u.v} / {u.max ?? "∞"}</span>
              </div>
              <div className="h-1.5 rounded-full" style={{ background: "var(--border)" }}>
                <div
                  className="h-full rounded-full"
                  style={{ background: "var(--primary)", width: u.max ? `${(u.v / u.max) * 100}%` : "30%", opacity: u.max ? 1 : 0.3 }}
                />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="tp-card overflow-hidden mb-6">
        <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
          <div className="font-semibold">Billing history</div>
        </div>
        <div className="px-5 py-8 text-sm text-center" style={{ color: "var(--subtle)" }}>
          Invoice history is available in the{" "}
          <button className="underline" style={{ color: "var(--primary-light)" }} onClick={() => openPortal.mutate()} disabled={portalBusy}>
            Stripe customer portal
          </button>
          .
        </div>
      </div>

      <div className="tp-card p-6" style={{ borderColor: "rgba(248,113,113,0.3)" }}>
        <div className="font-semibold mb-2" style={{ color: "var(--danger)" }}>Danger zone</div>
        <p className="text-sm mb-3" style={{ color: "var(--muted-foreground)" }}>
          You'll keep access until the end of your current billing cycle.
        </p>
        <button
          className="text-sm"
          style={{ color: "var(--danger)" }}
          onClick={() => openPortal.mutate()}
          disabled={portalBusy}
        >
          Cancel subscription via portal →
        </button>
      </div>
    </DashboardLayout>
  );
}
