import { createFileRoute, Link } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars, Pill } from "@/components/Stars";
import { testimonials, activity } from "@/lib/mock-data";
import { Send, LayoutGrid, ArrowRight, TrendingUp, Inbox, Star, Eye } from "lucide-react";

export const Route = createFileRoute("/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — TrustPanel" }] }),
  component: DashboardHome,
});

const metrics = [
  { label: "Total testimonials", value: "247", delta: "+8.4%", icon: Inbox },
  { label: "New this month", value: "34", delta: "+12 vs last", icon: TrendingUp },
  { label: "Average rating", value: "4.7", suffix: "★", delta: "+0.2", icon: Star },
  { label: "Widget impressions", value: "12,840", delta: "+24.1%", icon: Eye },
];

function DashboardHome() {
  const pending = testimonials.filter((t) => t.status === "pending");
  return (
    <DashboardLayout
      title="Good morning, Alex"
      action={
        <>
          <button className="tp-btn tp-btn-ghost">
            <Send size={14} /> Send request
          </button>
          <Link to="/dashboard/widgets/create" className="tp-btn tp-btn-primary">
            <LayoutGrid size={14} /> Create widget
          </Link>
        </>
      }
    >
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {metrics.map((m) => (
          <div key={m.label} className="tp-card p-5">
            <div className="flex justify-between items-start">
              <m.icon size={16} style={{ color: "var(--subtle)" }} />
              <Pill tone="success">{m.delta}</Pill>
            </div>
            <div className="mt-4 text-3xl font-semibold tracking-tight">
              {m.value}
              {m.suffix && <span className="ml-1" style={{ color: "var(--warning)" }}>{m.suffix}</span>}
            </div>
            <div className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
              {m.label}
            </div>
          </div>
        ))}
      </div>

      <div className="grid lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 tp-card">
          <div className="px-5 py-4 flex justify-between items-center border-b" style={{ borderColor: "var(--border)" }}>
            <div>
              <div className="font-semibold">Pending approval</div>
              <div className="text-xs mt-0.5" style={{ color: "var(--subtle)" }}>
                {pending.length} testimonials waiting for your review
              </div>
            </div>
            <Link to="/dashboard/testimonials" className="text-sm flex items-center gap-1" style={{ color: "var(--primary-light)" }}>
              View all <ArrowRight size={12} />
            </Link>
          </div>
          <div className="divide-y" style={{ borderColor: "var(--border)" }}>
            {pending.map((t) => (
              <div key={t.id} className="p-5 flex gap-4">
                <Avatar name={t.name} color={t.avatarColor} />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-medium text-sm">{t.name}</div>
                      <div className="text-xs" style={{ color: "var(--subtle)" }}>
                        {t.jobTitle} · {t.company}
                      </div>
                    </div>
                    <Stars value={t.rating} />
                  </div>
                  <p className="text-sm mt-2 line-clamp-2" style={{ color: "var(--muted-foreground)" }}>
                    {t.text}
                  </p>
                  <div className="flex gap-2 mt-3">
                    <button className="tp-btn tp-btn-success" style={{ padding: "6px 12px", fontSize: 12 }}>
                      Approve
                    </button>
                    <button className="tp-btn tp-btn-danger" style={{ padding: "6px 12px", fontSize: 12 }}>
                      Reject
                    </button>
                    <span className="ml-auto text-xs" style={{ color: "var(--subtle)" }}>
                      {t.date}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="tp-card">
          <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="font-semibold">Recent activity</div>
          </div>
          <div className="p-5 space-y-4">
            {activity.map((a, i) => (
              <div key={i} className="flex gap-3">
                <div className="w-2 h-2 rounded-full mt-1.5 shrink-0" style={{ background: a.color }} />
                <div className="text-sm flex-1">
                  <span className="font-medium">{a.who}</span>{" "}
                  <span style={{ color: "var(--muted-foreground)" }}>{a.what}</span>
                  <div className="text-xs mt-0.5" style={{ color: "var(--subtle)" }}>
                    {a.when}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
