import { createFileRoute, useSearch } from "@tanstack/react-router";
import { useQuery, useMutation } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Check, CreditCard } from "lucide-react";
import { billingApi } from "@/lib/api/billing";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/settings/billing")({
  head: () => ({ meta: [{ title: "Billing — TrustPanel" }] }),
  validateSearch: (s: Record<string, unknown>) => ({ checkout: s.checkout as string | undefined }),
  component: Billing,
});

function Billing() {
  useRequireAuth();
  const { data: me } = useMe();
  const { checkout } = useSearch({ from: "/dashboard/settings/billing" });

  const { data: plan, isLoading: planLoading } = useQuery({
    queryKey: ["billing-plan", me?.workspaceId],
    queryFn: () => billingApi.plan(me!.workspaceId!),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const openCheckout = useMutation({
    mutationFn: (priceId: string) => billingApi.checkout(priceId),
    onSuccess: ({ url }) => { window.location.href = url; },
  });

  const openPortal = useMutation({
    mutationFn: () => billingApi.portal(),
    onSuccess: ({ url }) => { window.location.href = url; },
  });

  const portalBusy = openPortal.isPending;

  const planName = plan?.planName ?? "Free";
  const isActive = plan?.status === "Active" || plan?.status === "Trialing" || plan == null;
  const renewsAt = plan?.currentPeriodEnd
    ? new Date(plan.currentPeriodEnd).toLocaleDateString("en-US", { month: "long", day: "numeric", year: "numeric" })
    : null;

  const planFeatures: string[] = [
    ...(plan?.hasVideoTestimonials ? ["Video testimonials"] : []),
    ...(plan?.hasAiFeatures ? ["AI insights"] : []),
    ...(plan?.hasApiAccess ? ["Public REST API"] : []),
    ...(plan?.hasCustomDomain ? ["Custom domain"] : []),
    ...(plan?.hasWhiteLabel ? ["White-label"] : []),
    ...(plan?.hasTeamMembers ? ["Team members"] : []),
  ];

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
              <Pill tone={plan?.isTrialing ? "warning" : "primary"}>
                {plan?.isTrialing ? "TRIAL" : "CURRENT PLAN"}
              </Pill>
              <div className="mt-3 text-2xl font-semibold">
                {planLoading ? <span className="animate-pulse">…</span> : planName}
              </div>
              {plan?.monthlyPrice != null && plan.monthlyPrice > 0 && (
                <div className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
                  ${plan.monthlyPrice}/mo{renewsAt ? ` · renews ${renewsAt}` : ""}
                  {plan.cancelAtPeriodEnd && (
                    <span style={{ color: "var(--danger)" }}> · cancels at period end</span>
                  )}
                </div>
              )}
              {plan?.status === "PastDue" && (
                <div className="text-sm mt-1" style={{ color: "var(--danger)" }}>
                  Payment past due — please update your payment method.
                </div>
              )}
            </div>
            <button
              className="tp-btn tp-btn-primary"
              onClick={() => openPortal.mutate()}
              disabled={portalBusy}
            >
              {portalBusy ? "Redirecting…" : "Manage plan"}
            </button>
          </div>

          {planFeatures.length > 0 && (
            <div className="grid grid-cols-2 gap-y-2 mt-5 text-sm">
              {planFeatures.map((f) => (
                <div key={f} className="flex items-center gap-2">
                  <Check size={14} style={{ color: "var(--success)" }} />
                  <span>{f}</span>
                </div>
              ))}
            </div>
          )}

          {!planLoading && planName === "Free" && (
            <div className="mt-4 pt-4" style={{ borderTop: "1px solid var(--border)" }}>
              <div className="text-sm mb-3" style={{ color: "var(--muted-foreground)" }}>
                Upgrade to unlock more features.
              </div>
              <button
                className="tp-btn tp-btn-primary"
                onClick={() => openCheckout.mutate("price_starter_monthly")}
                disabled={openCheckout.isPending}
              >
                {openCheckout.isPending ? "Redirecting…" : "Upgrade to Starter"}
              </button>
            </div>
          )}
        </div>

        <div className="tp-card p-6">
          <div className="font-semibold mb-4">Payment method</div>
          <div className="tp-card p-4 flex items-center gap-3" style={{ background: "var(--surface)" }}>
            <div className="w-10 h-7 rounded flex items-center justify-center" style={{ background: "linear-gradient(135deg,#7c6af7,#60a5fa)" }}>
              <CreditCard size={14} color="white" />
            </div>
            <div className="flex-1 text-sm" style={{ color: "var(--muted-foreground)" }}>
              Manage via Stripe portal
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

      {plan && (
        <div className="tp-card p-6 mb-6">
          <div className="font-semibold mb-4">Plan limits</div>
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-5">
            {[
              { l: "Testimonials", v: null, max: plan.testimonialLimit },
              { l: "Widgets", v: null, max: plan.widgetLimit },
            ].map((u) => (
              <div key={u.l}>
                <div className="flex justify-between text-sm mb-2">
                  <span>{u.l}</span>
                  <span style={{ color: "var(--subtle)" }}>{u.max === -1 ? "Unlimited" : u.max}</span>
                </div>
                <div className="h-1.5 rounded-full" style={{ background: "var(--border)" }}>
                  <div
                    className="h-full rounded-full"
                    style={{ background: "var(--primary)", width: "30%", opacity: 0.3 }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

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
