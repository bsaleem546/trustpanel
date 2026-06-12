import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars, Pill } from "@/components/Stars";
import { testimonials, type TestimonialStatus } from "@/lib/mock-data";
import { Search, Filter, Play, MoreVertical, Star } from "lucide-react";

export const Route = createFileRoute("/dashboard/testimonials")({
  head: () => ({ meta: [{ title: "Testimonials — TrustPanel" }] }),
  component: TestimonialsInbox,
});

const tabs: { label: string; key: "all" | TestimonialStatus; count: number }[] = [
  { label: "All", key: "all", count: testimonials.length },
  { label: "Pending", key: "pending", count: testimonials.filter((t) => t.status === "pending").length },
  { label: "Approved", key: "approved", count: testimonials.filter((t) => t.status === "approved").length },
  { label: "Featured", key: "featured", count: testimonials.filter((t) => t.status === "featured").length },
  { label: "Rejected", key: "rejected", count: testimonials.filter((t) => t.status === "rejected").length },
];

const sentimentTone = { positive: "success", neutral: "warning", negative: "danger" } as const;

function TestimonialsInbox() {
  const [tab, setTab] = useState<(typeof tabs)[number]["key"]>("all");
  const [selected, setSelected] = useState<string[]>([]);
  const visible = testimonials.filter((t) => tab === "all" || t.status === tab);

  return (
    <DashboardLayout title="Testimonials">
      <div className="tp-card p-4 mb-4">
        <div className="flex flex-wrap gap-2 items-center">
          <div className="relative flex-1 min-w-[220px]">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--subtle)" }} />
            <input className="tp-input pl-9" placeholder="Search by name, company, or text…" />
          </div>
          {[
            { l: "Rating", o: "Any" },
            { l: "Type", o: "All" },
            { l: "Source", o: "All" },
            { l: "Tag", o: "All" },
            { l: "Sort", o: "Newest" },
          ].map((f) => (
            <button key={f.l} className="tp-btn tp-btn-ghost text-xs" style={{ padding: "8px 10px" }}>
              <Filter size={12} /> {f.l}: {f.o}
            </button>
          ))}
        </div>
        <div className="flex gap-1 mt-4">
          {tabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className="px-3 py-1.5 rounded-md text-sm flex items-center gap-2"
              style={{
                background: tab === t.key ? "var(--primary-soft)" : "transparent",
                color: tab === t.key ? "var(--primary-light)" : "var(--muted-foreground)",
              }}
            >
              {t.label}
              <Pill tone={tab === t.key ? "primary" : "neutral"}>{t.count}</Pill>
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
            <button className="tp-btn tp-btn-success" style={{ padding: "6px 12px", fontSize: 12 }}>Approve all</button>
            <button className="tp-btn tp-btn-danger" style={{ padding: "6px 12px", fontSize: 12 }}>Reject all</button>
            <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 12px", fontSize: 12 }}>Add tag</button>
          </div>
        </div>
      )}

      <div className="space-y-3">
        {visible.map((t) => (
          <div key={t.id} className="tp-card p-5 flex gap-4">
            <input
              type="checkbox"
              checked={selected.includes(t.id)}
              onChange={(e) => setSelected(e.target.checked ? [...selected, t.id] : selected.filter((x) => x !== t.id))}
              className="mt-1"
            />
            <Avatar name={t.name} color={t.avatarColor} />
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between gap-3 flex-wrap">
                <div>
                  <Link to="/dashboard/testimonials/$id" params={{ id: t.id }} className="font-medium hover:underline">
                    {t.name}
                  </Link>
                  <span className="text-sm ml-2" style={{ color: "var(--subtle)" }}>
                    {t.jobTitle} · {t.company}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <Stars value={t.rating} />
                  <Pill tone="neutral">{t.source}</Pill>
                  <Pill tone={sentimentTone[t.sentiment]}>{t.sentiment}</Pill>
                  <Pill
                    tone={
                      t.status === "approved"
                        ? "success"
                        : t.status === "featured"
                          ? "primary"
                          : t.status === "rejected"
                            ? "danger"
                            : "warning"
                    }
                  >
                    {t.status}
                  </Pill>
                </div>
              </div>
              {t.type === "video" ? (
                <div className="mt-3 flex items-center gap-3">
                  <div
                    className="rounded-lg flex items-center justify-center"
                    style={{ width: 140, height: 80, background: "var(--surface)", border: "1px solid var(--border)" }}
                  >
                    <Play size={20} style={{ color: "var(--primary-light)" }} />
                  </div>
                  <div>
                    <Pill tone="info">VIDEO · {t.videoDuration}</Pill>
                    <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
                      {t.text}
                    </p>
                  </div>
                </div>
              ) : (
                <p className="text-sm mt-2 line-clamp-2" style={{ color: "var(--muted-foreground)" }}>
                  {t.text}
                </p>
              )}
              <div className="mt-3 flex flex-wrap items-center gap-2">
                {t.tags.map((tag) => (
                  <Pill key={tag} tone="neutral">
                    #{tag}
                  </Pill>
                ))}
                <span className="text-xs ml-auto" style={{ color: "var(--subtle)" }}>
                  {t.date}
                </span>
                <button className="tp-btn tp-btn-ghost" style={{ padding: "4px 10px", fontSize: 12 }}>
                  <Star size={12} /> Feature
                </button>
                <button className="tp-btn tp-btn-ghost" style={{ padding: "4px 8px" }}>
                  <MoreVertical size={12} />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="flex justify-between items-center mt-6 text-sm" style={{ color: "var(--muted-foreground)" }}>
        <span>Showing 1–8 of 247</span>
        <div className="flex gap-1">
          <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 10px" }}>Previous</button>
          <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 10px" }}>Next</button>
        </div>
      </div>
    </DashboardLayout>
  );
}
