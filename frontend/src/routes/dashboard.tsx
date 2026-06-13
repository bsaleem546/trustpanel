import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Stars, Pill } from "@/components/Stars";
import { testimonialsApi, type Testimonial } from "@/lib/api/testimonials";
import { useMe, useRequireAuth } from "@/lib/auth";
import { Send, LayoutGrid, ArrowRight, TrendingUp, Inbox, Star, Eye } from "lucide-react";

export const Route = createFileRoute("/dashboard")({
  head: () => ({ meta: [{ title: "Dashboard — TrustPanel" }] }),
  component: DashboardHome,
});

function greet(email?: string) {
  const h = new Date().getHours();
  const time = h < 12 ? "Good morning" : h < 18 ? "Good afternoon" : "Good evening";
  const name = email ? email.split("@")[0] : "";
  return name ? `${time}, ${name}` : time;
}

function DashboardHome() {
  useRequireAuth();
  const { data: me } = useMe();
  const queryClient = useQueryClient();

  const { data: pendingData } = useQuery({
    queryKey: ["testimonials", me?.workspaceId, "Pending", 1],
    queryFn: () => testimonialsApi.list(me!.workspaceId!, { status: "Pending", pageSize: 5 }),
    enabled: !!me?.workspaceId,
    staleTime: 30_000,
  });

  const { data: allData } = useQuery({
    queryKey: ["testimonials", me?.workspaceId, "all", 1],
    queryFn: () => testimonialsApi.list(me!.workspaceId!, { pageSize: 1 }),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["testimonials"] });
  const approve = useMutation({ mutationFn: (id: string) => testimonialsApi.approve(id), onSuccess: invalidate });
  const reject = useMutation({ mutationFn: (id: string) => testimonialsApi.reject(id), onSuccess: invalidate });

  const pending: Testimonial[] = pendingData?.items ?? [];
  const total = allData?.total ?? 0;

  const metrics = [
    { label: "Total testimonials", value: String(total), delta: null, icon: Inbox },
    { label: "Pending review", value: String(pendingData?.total ?? 0), delta: null, icon: TrendingUp },
    { label: "Average rating", value: "—", suffix: "★", delta: null, icon: Star },
    { label: "Widget impressions", value: "—", delta: null, icon: Eye },
  ];

  return (
    <DashboardLayout
      title={greet(me?.email)}
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
                {pendingData?.total ?? 0} testimonials waiting for your review
              </div>
            </div>
            <Link to="/dashboard/testimonials" className="text-sm flex items-center gap-1" style={{ color: "var(--primary-light)" }}>
              View all <ArrowRight size={12} />
            </Link>
          </div>
          <div className="divide-y" style={{ borderColor: "var(--border)" }}>
            {pending.map((t) => (
              <div key={t.id} className="p-5 flex gap-4">
                <div
                  className="w-9 h-9 rounded-full flex items-center justify-center text-xs font-medium shrink-0"
                  style={{ background: "rgba(124,106,247,0.18)", color: "var(--primary-light)" }}
                >
                  {t.submitter.name.slice(0, 2).toUpperCase()}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-medium text-sm">{t.submitter.name}</div>
                      {(t.submitter.jobTitle || t.submitter.company) && (
                        <div className="text-xs" style={{ color: "var(--subtle)" }}>
                          {[t.submitter.jobTitle, t.submitter.company].filter(Boolean).join(" · ")}
                        </div>
                      )}
                    </div>
                    {t.rating && <Stars value={t.rating} />}
                  </div>
                  <p className="text-sm mt-2 line-clamp-2" style={{ color: "var(--muted-foreground)" }}>
                    {t.content}
                  </p>
                  <div className="flex gap-2 mt-3">
                    <button
                      className="tp-btn tp-btn-success"
                      style={{ padding: "6px 12px", fontSize: 12 }}
                      onClick={() => approve.mutate(t.id)}
                      disabled={approve.isPending}
                    >
                      Approve
                    </button>
                    <button
                      className="tp-btn tp-btn-danger"
                      style={{ padding: "6px 12px", fontSize: 12 }}
                      onClick={() => reject.mutate(t.id)}
                      disabled={reject.isPending}
                    >
                      Reject
                    </button>
                    <span className="ml-auto text-xs" style={{ color: "var(--subtle)" }}>
                      {new Date(t.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              </div>
            ))}
            {pending.length === 0 && (
              <div className="p-8 text-center text-sm" style={{ color: "var(--subtle)" }}>
                No pending testimonials. 🎉
              </div>
            )}
          </div>
        </div>

        <div className="tp-card">
          <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="font-semibold">Quick stats</div>
          </div>
          <div className="p-5 space-y-4">
            <div className="flex justify-between text-sm">
              <span style={{ color: "var(--muted-foreground)" }}>Total testimonials</span>
              <span className="font-medium">{total}</span>
            </div>
            <div className="flex justify-between text-sm">
              <span style={{ color: "var(--muted-foreground)" }}>Pending review</span>
              <Pill tone="warning">{pendingData?.total ?? 0}</Pill>
            </div>
            <div className="flex justify-between text-sm">
              <span style={{ color: "var(--muted-foreground)" }}>Analytics</span>
              <Link to="/dashboard/analytics" className="text-xs" style={{ color: "var(--primary-light)" }}>
                View →
              </Link>
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
