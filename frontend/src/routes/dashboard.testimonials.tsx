import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Stars, Pill } from "@/components/Stars";
import { testimonialsApi, type TestimonialStatus, type Testimonial } from "@/lib/api/testimonials";
import { useMe, useRequireAuth } from "@/lib/auth";
import { Search, Filter, Play, MoreVertical, Star } from "lucide-react";

export const Route = createFileRoute("/dashboard/testimonials")({
  head: () => ({ meta: [{ title: "Testimonials — TrustPanel" }] }),
  component: TestimonialsInbox,
});

const STATUS_TABS: { label: string; key: "all" | TestimonialStatus }[] = [
  { label: "All", key: "all" },
  { label: "Pending", key: "Pending" },
  { label: "Approved", key: "Approved" },
  { label: "Rejected", key: "Rejected" },
];

const sentimentTone = (score: number | null) => {
  if (score === null) return "neutral" as const;
  return score > 0.3 ? "success" as const : score < -0.2 ? "danger" as const : "warning" as const;
};

const statusTone = (s: TestimonialStatus) =>
  s === "Approved" ? "success" as const : s === "Rejected" ? "danger" as const : "warning" as const;

function TestimonialsInbox() {
  useRequireAuth();
  const { data: me } = useMe();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<"all" | TestimonialStatus>("all");
  const [selected, setSelected] = useState<string[]>([]);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ["testimonials", me?.workspaceId, tab, page],
    queryFn: () =>
      testimonialsApi.list(me!.workspaceId!, {
        status: tab === "all" ? undefined : tab,
        page,
        pageSize: 25,
      }),
    enabled: !!me?.workspaceId,
    staleTime: 30_000,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["testimonials"] });
    setSelected([]);
  };

  const approve = useMutation({ mutationFn: (id: string) => testimonialsApi.approve(id), onSuccess: invalidate });
  const reject = useMutation({ mutationFn: (id: string) => testimonialsApi.reject(id), onSuccess: invalidate });
  const feature = useMutation({
    mutationFn: ({ id, v }: { id: string; v: boolean }) => testimonialsApi.feature(id, v),
    onSuccess: invalidate,
  });
  const batch = useMutation({
    mutationFn: (action: "Approve" | "Reject" | "Delete") =>
      testimonialsApi.batch(me!.workspaceId!, selected, action),
    onSuccess: invalidate,
  });

  const items: Testimonial[] = data?.items ?? [];
  const total = data?.total ?? 0;
  const pageSize = data?.pageSize ?? 25;

  return (
    <DashboardLayout title="Testimonials">
      <div className="tp-card p-4 mb-4">
        <div className="flex flex-wrap gap-2 items-center">
          <div className="relative flex-1 min-w-[220px]">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--subtle)" }} />
            <input className="tp-input pl-9" placeholder="Search by name, company, or text…" />
          </div>
          <button className="tp-btn tp-btn-ghost text-xs" style={{ padding: "8px 10px" }}>
            <Filter size={12} /> Filter
          </button>
        </div>
        <div className="flex gap-1 mt-4">
          {STATUS_TABS.map((t) => (
            <button
              key={t.key}
              onClick={() => { setTab(t.key); setPage(1); setSelected([]); }}
              className="px-3 py-1.5 rounded-md text-sm"
              style={{
                background: tab === t.key ? "var(--primary-soft)" : "transparent",
                color: tab === t.key ? "var(--primary-light)" : "var(--muted-foreground)",
              }}
            >
              {t.label}
            </button>
          ))}
        </div>
      </div>

      {selected.length > 0 && (
        <div
          className="tp-card flex items-center px-4 py-2 mb-4 gap-3 text-sm"
          style={{ background: "var(--primary-soft)", borderColor: "var(--primary)" }}
        >
          <span>{selected.length} selected</span>
          <div className="flex gap-2 ml-auto">
            <button className="tp-btn tp-btn-success" style={{ padding: "6px 12px", fontSize: 12 }} onClick={() => batch.mutate("Approve")}>Approve all</button>
            <button className="tp-btn tp-btn-danger" style={{ padding: "6px 12px", fontSize: 12 }} onClick={() => batch.mutate("Reject")}>Reject all</button>
          </div>
        </div>
      )}

      {isLoading && <div className="text-sm mb-4" style={{ color: "var(--subtle)" }}>Loading…</div>}

      <div className="space-y-3">
        {items.map((t) => (
          <div key={t.id} className="tp-card p-5 flex gap-4">
            <input
              type="checkbox"
              checked={selected.includes(t.id)}
              onChange={(e) => setSelected(e.target.checked ? [...selected, t.id] : selected.filter((x) => x !== t.id))}
              className="mt-1"
            />
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-3 flex-wrap">
                <div>
                  <Link to="/dashboard/testimonials/$id" params={{ id: t.id }} className="font-medium hover:underline">
                    {t.submitter.name}
                  </Link>
                  {(t.submitter.jobTitle || t.submitter.company) && (
                    <span className="text-sm ml-2" style={{ color: "var(--subtle)" }}>
                      {[t.submitter.jobTitle, t.submitter.company].filter(Boolean).join(" · ")}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {t.rating && <Stars value={t.rating} />}
                  <Pill tone="neutral">{t.source}</Pill>
                  {t.sentimentScore !== null && (
                    <Pill tone={sentimentTone(t.sentimentScore)}>
                      {t.sentimentScore > 0.3 ? "positive" : t.sentimentScore < -0.2 ? "negative" : "neutral"}
                    </Pill>
                  )}
                  <Pill tone={statusTone(t.status)}>{t.status}</Pill>
                </div>
              </div>
              {t.type === "Video" ? (
                <div className="mt-3 flex items-center gap-3">
                  <div
                    className="rounded-lg flex items-center justify-center"
                    style={{ width: 140, height: 80, background: "var(--surface)", border: "1px solid var(--border)" }}
                  >
                    <Play size={20} style={{ color: "var(--primary-light)" }} />
                  </div>
                  <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>{t.content}</p>
                </div>
              ) : (
                <p className="text-sm mt-2 line-clamp-2" style={{ color: "var(--muted-foreground)" }}>{t.content}</p>
              )}
              <div className="mt-3 flex flex-wrap items-center gap-2">
                {t.tags.map((tag) => (
                  <Pill key={tag} tone="neutral">#{tag}</Pill>
                ))}
                <span className="text-xs ml-auto" style={{ color: "var(--subtle)" }}>
                  {new Date(t.createdAt).toLocaleDateString()}
                </span>
                {t.status === "Pending" && (
                  <>
                    <button className="tp-btn tp-btn-success" style={{ padding: "4px 10px", fontSize: 12 }} onClick={() => approve.mutate(t.id)}>Approve</button>
                    <button className="tp-btn tp-btn-danger" style={{ padding: "4px 10px", fontSize: 12 }} onClick={() => reject.mutate(t.id)}>Reject</button>
                  </>
                )}
                <button
                  className="tp-btn tp-btn-ghost"
                  style={{ padding: "4px 10px", fontSize: 12 }}
                  onClick={() => feature.mutate({ id: t.id, v: !t.featuredAt })}
                >
                  <Star size={12} /> {t.featuredAt ? "Unfeature" : "Feature"}
                </button>
                <button className="tp-btn tp-btn-ghost" style={{ padding: "4px 8px" }}>
                  <MoreVertical size={12} />
                </button>
              </div>
            </div>
          </div>
        ))}
        {!isLoading && items.length === 0 && (
          <div className="tp-card p-10 text-center" style={{ color: "var(--subtle)" }}>No testimonials found.</div>
        )}
      </div>

      {total > pageSize && (
        <div className="flex justify-between items-center mt-6 text-sm" style={{ color: "var(--muted-foreground)" }}>
          <span>Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, total)} of {total}</span>
          <div className="flex gap-1">
            <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 10px" }} onClick={() => setPage(Math.max(1, page - 1))} disabled={page === 1}>Previous</button>
            <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 10px" }} onClick={() => setPage(page + 1)} disabled={page * pageSize >= total}>Next</button>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}
