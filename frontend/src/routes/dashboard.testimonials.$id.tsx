import { createFileRoute, Link } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars, Pill } from "@/components/Stars";
import { testimonials } from "@/lib/mock-data";
import { ArrowLeft, Copy, Edit, Sparkles, Mail, Image as ImageIcon, Play } from "lucide-react";

export const Route = createFileRoute("/dashboard/testimonials/$id")({
  head: () => ({ meta: [{ title: "Testimonial — TrustPanel" }] }),
  component: TestimonialDetail,
});

function TestimonialDetail() {
  const { id } = Route.useParams();
  const t = testimonials.find((x) => x.id === id) ?? testimonials[0];
  const sentScore = t.sentiment === "positive" ? 0.84 : t.sentiment === "neutral" ? 0.12 : -0.45;

  return (
    <DashboardLayout>
      <Link to="/dashboard/testimonials" className="text-sm flex items-center gap-1 mb-4" style={{ color: "var(--muted-foreground)" }}>
        <ArrowLeft size={14} /> Back to inbox
      </Link>
      <div className="grid lg:grid-cols-[1fr_360px] gap-6">
        <div className="space-y-4">
          <div className="tp-card p-6">
            <div className="flex justify-between items-start mb-4">
              <Stars value={t.rating} size={18} />
              <button className="tp-btn tp-btn-ghost" style={{ padding: "6px 10px", fontSize: 12 }}>
                <Edit size={12} /> Edit
              </button>
            </div>
            {t.type === "video" ? (
              <div className="rounded-lg overflow-hidden" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
                <div className="aspect-video flex items-center justify-center">
                  <div className="w-14 h-14 rounded-full flex items-center justify-center" style={{ background: "rgba(255,255,255,0.1)" }}>
                    <Play size={22} style={{ color: "white" }} />
                  </div>
                </div>
                <div className="p-3 border-t" style={{ borderColor: "var(--border)" }}>
                  <div className="text-xs mb-2" style={{ color: "var(--subtle)" }}>
                    Trim · 0:00 → {t.videoDuration}
                  </div>
                  <div className="h-1.5 rounded-full" style={{ background: "var(--border)" }}>
                    <div className="h-full rounded-full" style={{ background: "var(--primary)", width: "100%" }} />
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-lg leading-relaxed">{t.text}</p>
            )}
          </div>

          <div className="tp-card p-5">
            <div className="flex items-center gap-2 mb-2">
              <Sparkles size={14} style={{ color: "var(--primary-light)" }} />
              <span className="text-xs uppercase tracking-wider" style={{ color: "var(--primary-light)" }}>
                AI highlight
              </span>
            </div>
            <div className="text-sm" style={{ color: "var(--muted-foreground)" }}>
              Strongest sentence:
            </div>
            <p className="mt-2 italic">"{t.text.split(".")[0]}."</p>
            <button className="tp-btn tp-btn-ghost mt-3" style={{ padding: "6px 10px", fontSize: 12 }}>
              <Copy size={12} /> Copy quote
            </button>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <button className="tp-btn tp-btn-ghost">
              <ImageIcon size={14} /> Generate quote card
            </button>
            <button className="tp-btn tp-btn-ghost">
              <Mail size={14} /> Send thank-you reply
            </button>
          </div>
        </div>

        <div className="space-y-4">
          <div className="tp-card p-5">
            <div className="flex items-center gap-3">
              <Avatar name={t.name} color={t.avatarColor} size={48} />
              <div>
                <div className="font-medium">{t.name}</div>
                <div className="text-xs" style={{ color: "var(--subtle)" }}>
                  {t.jobTitle} · {t.company}
                </div>
              </div>
            </div>
            <div className="mt-4 grid grid-cols-2 gap-2 text-xs">
              <Detail label="Source" value={t.source} />
              <Detail label="Submitted" value={t.date} />
              <Detail label="Country" value={t.country ?? "—"} />
              <Detail label="Type" value={t.type} />
            </div>
          </div>

          <div className="tp-card p-5">
            <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--subtle)" }}>
              Status
            </div>
            <select className="tp-input" defaultValue={t.status}>
              <option value="pending">Pending</option>
              <option value="approved">Approved</option>
              <option value="featured">Featured</option>
              <option value="rejected">Rejected</option>
            </select>
            <div className="flex gap-2 mt-3">
              <button className="tp-btn tp-btn-success flex-1" style={{ padding: "8px", fontSize: 12 }}>Approve</button>
              <button className="tp-btn tp-btn-danger flex-1" style={{ padding: "8px", fontSize: 12 }}>Reject</button>
            </div>
          </div>

          <div className="tp-card p-5">
            <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--subtle)" }}>
              Tags
            </div>
            <div className="flex flex-wrap gap-2">
              {t.tags.map((tag) => (
                <Pill key={tag} tone="neutral">
                  #{tag}
                </Pill>
              ))}
              <button className="text-xs" style={{ color: "var(--primary-light)" }}>
                + Add tag
              </button>
            </div>
          </div>

          <div className="tp-card p-5">
            <div className="text-xs uppercase tracking-wider mb-4" style={{ color: "var(--subtle)" }}>
              Sentiment score
            </div>
            <SentGauge score={sentScore} />
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div style={{ color: "var(--subtle)" }}>{label}</div>
      <div className="mt-0.5 font-medium" style={{ color: "var(--foreground)", textTransform: "capitalize" }}>
        {value}
      </div>
    </div>
  );
}

function SentGauge({ score }: { score: number }) {
  const pct = ((score + 1) / 2) * 100;
  const color = score > 0.3 ? "var(--success)" : score < -0.2 ? "var(--danger)" : "var(--warning)";
  return (
    <div>
      <div className="flex justify-between text-xs mb-2" style={{ color: "var(--subtle)" }}>
        <span>−1</span>
        <span>0</span>
        <span>+1</span>
      </div>
      <div className="relative h-2 rounded-full" style={{ background: "var(--border)" }}>
        <div className="absolute h-full" style={{ width: `${pct}%`, background: color, borderRadius: 999 }} />
      </div>
      <div className="mt-3 flex items-baseline gap-2">
        <span className="text-3xl font-semibold" style={{ color }}>
          {score.toFixed(2)}
        </span>
        <span className="text-sm capitalize" style={{ color: "var(--muted-foreground)" }}>
          {score > 0.3 ? "positive" : score < -0.2 ? "negative" : "neutral"}
        </span>
      </div>
    </div>
  );
}
