import { createFileRoute } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Check, Download, CreditCard } from "lucide-react";

export const Route = createFileRoute("/dashboard/settings/billing")({
  head: () => ({ meta: [{ title: "Billing — TrustPanel" }] }),
  component: Billing,
});

const usage = [
  { l: "Workspaces", v: 2, max: 10 },
  { l: "Testimonials", v: 247, max: null, label: "unlimited" },
  { l: "Widgets", v: 4, max: 10 },
  { l: "Team members", v: 3, max: 10 },
];

const history = [
  { date: "Mar 12, 2026", desc: "Agency plan · monthly", amount: "$119.00", status: "Paid" },
  { date: "Feb 12, 2026", desc: "Agency plan · monthly", amount: "$119.00", status: "Paid" },
  { date: "Jan 12, 2026", desc: "Agency plan · monthly", amount: "$119.00", status: "Paid" },
  { date: "Dec 12, 2025", desc: "Pro plan · monthly", amount: "$59.00", status: "Paid" },
];

function Billing() {
  return (
    <DashboardLayout title="Billing">
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
            <button className="tp-btn tp-btn-primary">Upgrade plan</button>
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
              <div className="text-xs" style={{ color: "var(--subtle)" }}>
                Expires 09/28
              </div>
            </div>
          </div>
          <button className="tp-btn tp-btn-ghost w-full mt-3">Update payment method</button>
        </div>
      </div>

      <div className="tp-card p-6 mb-6">
        <div className="font-semibold mb-4">Usage this billing cycle</div>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-5">
          {usage.map((u) => (
            <div key={u.l}>
              <div className="flex justify-between text-sm mb-2">
                <span>{u.l}</span>
                <span style={{ color: "var(--subtle)" }}>
                  {u.v} / {u.max ?? u.label}
                </span>
              </div>
              <div className="h-1.5 rounded-full" style={{ background: "var(--border)" }}>
                <div
                  className="h-full rounded-full"
                  style={{
                    background: "var(--primary)",
                    width: u.max ? `${(u.v / u.max) * 100}%` : "100%",
                    opacity: u.max ? 1 : 0.3,
                  }}
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
        <table className="w-full text-sm">
          <tbody>
            {history.map((h, i) => (
              <tr key={i} style={{ borderTop: i ? "1px solid var(--border)" : "none" }}>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>{h.date}</td>
                <td className="px-5 py-3">{h.desc}</td>
                <td className="px-5 py-3 font-medium">{h.amount}</td>
                <td className="px-5 py-3"><Pill tone="success">{h.status}</Pill></td>
                <td className="px-5 py-3 text-right">
                  <button className="text-xs flex items-center gap-1 ml-auto" style={{ color: "var(--primary-light)" }}>
                    <Download size={11} /> Invoice
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="tp-card p-6" style={{ borderColor: "rgba(248,113,113,0.3)" }}>
        <div className="font-semibold mb-2" style={{ color: "var(--danger)" }}>
          Danger zone
        </div>
        <p className="text-sm mb-3" style={{ color: "var(--muted-foreground)" }}>
          You'll keep access until the end of your current billing cycle.
        </p>
        <button className="text-sm" style={{ color: "var(--danger)" }}>
          Cancel subscription
        </button>
      </div>
    </DashboardLayout>
  );
}
