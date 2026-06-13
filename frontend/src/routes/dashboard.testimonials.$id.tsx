import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Stars, Pill } from "@/components/Stars";
import { testimonialsApi } from "@/lib/api/testimonials";
import { useRequireAuth } from "@/lib/auth";
import { ArrowLeft, Copy, Play, Sparkles } from "lucide-react";

export const Route = createFileRoute("/dashboard/testimonials/$id")({
  head: () => ({ meta: [{ title: "Testimonial — TrustPanel" }] }),
  component: TestimonialDetail,
});

function TestimonialDetail() {
  useRequireAuth();
  const { id } = Route.useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: t, isLoading, isError } = useQuery({
    queryKey: ["testimonial", id],
    queryFn: () => testimonialsApi.get(id),
    staleTime: 30_000,
  });

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["testimonial", id] });
    queryClient.invalidateQueries({ queryKey: ["testimonials"] });
  };

  const approve = useMutation({ mutationFn: () => testimonialsApi.approve(id), onSuccess: invalidate });
  const reject = useMutation({
    mutationFn: () => testimonialsApi.reject(id),
    onSuccess: () => { invalidate(); navigate({ to: "/dashboard/testimonials" }); },
  });
  const feature = useMutation({
    mutationFn: (v: boolean) => testimonialsApi.feature(id, v),
    onSuccess: invalidate,
  });

  const [tagInput, setTagInput] = useState("");
  const updateTags = useMutation({ mutationFn: (tags: string[]) => testimonialsApi.updateTags(id, tags), onSuccess: invalidate });

  if (isLoading) {
    return (
      <DashboardLayout>
        <div className="text-sm" style={{ color: "var(--subtle)" }}>Loading…</div>
      </DashboardLayout>
    );
  }
  if (isError || !t) {
    return (
      <DashboardLayout>
        <div className="text-sm" style={{ color: "var(--danger)" }}>Testimonial not found.</div>
      </DashboardLayout>
    );
  }

  const sentScore = t.sentimentScore ?? 0;

  return (
    <DashboardLayout>
      <Link to="/dashboard/testimonials" className="text-sm flex items-center gap-1 mb-4" style={{ color: "var(--muted-foreground)" }}>
        <ArrowLeft size={14} /> Back to inbox
      </Link>
      <div className="grid lg:grid-cols-[1fr_360px] gap-6">
        <div className="space-y-4">
          <div className="tp-card p-6">
            <div className="flex justify-between items-start mb-4">
              {t.rating && <Stars value={t.rating} size={18} />}
              <div className="flex gap-2 ml-auto">
                <Pill tone={t.status === "Approved" ? "success" : t.status === "Rejected" ? "danger" : "warning"}>
                  {t.status}
                </Pill>
                {t.featuredAt && <Pill tone="primary">Featured</Pill>}
              </div>
            </div>
            {t.type === "Video" ? (
              <div className="rounded-lg overflow-hidden" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
                <div className="aspect-video flex items-center justify-center">
                  <div className="w-14 h-14 rounded-full flex items-center justify-center" style={{ background: "rgba(255,255,255,0.1)" }}>
                    <Play size={22} style={{ color: "white" }} />
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-lg leading-relaxed">{t.content}</p>
            )}
          </div>

          {t.highlight && (
            <div className="tp-card p-5">
              <div className="flex items-center gap-2 mb-2">
                <Sparkles size={14} style={{ color: "var(--primary-light)" }} />
                <span className="text-xs uppercase tracking-wider" style={{ color: "var(--primary-light)" }}>AI highlight</span>
              </div>
              <p className="mt-2 italic">"{t.highlight}"</p>
              <button
                className="tp-btn tp-btn-ghost mt-3"
                style={{ padding: "6px 10px", fontSize: 12 }}
                onClick={() => navigator.clipboard?.writeText(t.highlight!)}
              >
                <Copy size={12} /> Copy quote
              </button>
            </div>
          )}
        </div>

        <div className="space-y-4">
          <div className="tp-card p-5">
            <div className="font-medium">{t.submitter.name}</div>
            {(t.submitter.jobTitle || t.submitter.company) && (
              <div className="text-xs mt-0.5" style={{ color: "var(--subtle)" }}>
                {[t.submitter.jobTitle, t.submitter.company].filter(Boolean).join(" · ")}
              </div>
            )}
            {t.submitter.email && (
              <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>{t.submitter.email}</div>
            )}
            <div className="mt-4 grid grid-cols-2 gap-2 text-xs">
              <Detail label="Source" value={t.source} />
              <Detail label="Submitted" value={new Date(t.createdAt).toLocaleDateString()} />
              <Detail label="Type" value={t.type} />
            </div>
          </div>

          <div className="tp-card p-5">
            <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--subtle)" }}>Actions</div>
            <div className="flex gap-2">
              {t.status !== "Approved" && (
                <button className="tp-btn tp-btn-success flex-1" style={{ padding: "8px", fontSize: 12 }} onClick={() => approve.mutate()} disabled={approve.isPending}>
                  Approve
                </button>
              )}
              {t.status !== "Rejected" && (
                <button className="tp-btn tp-btn-danger flex-1" style={{ padding: "8px", fontSize: 12 }} onClick={() => reject.mutate()} disabled={reject.isPending}>
                  Reject
                </button>
              )}
              <button
                className="tp-btn tp-btn-ghost flex-1"
                style={{ padding: "8px", fontSize: 12 }}
                onClick={() => feature.mutate(!t.featuredAt)}
                disabled={feature.isPending}
              >
                {t.featuredAt ? "Unfeature" : "Feature"}
              </button>
            </div>
          </div>

          <div className="tp-card p-5">
            <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--subtle)" }}>Tags</div>
            <div className="flex flex-wrap gap-2 mb-2">
              {t.tags.map((tag) => (
                <button
                  key={tag}
                  className="text-xs"
                  style={{ color: "var(--subtle)" }}
                  onClick={() => updateTags.mutate(t.tags.filter((x) => x !== tag))}
                >
                  <Pill tone="neutral">#{tag} ×</Pill>
                </button>
              ))}
            </div>
            <div className="flex gap-2">
              <input
                className="tp-input text-xs"
                placeholder="Add tag"
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && tagInput.trim()) {
                    updateTags.mutate([...t.tags, tagInput.trim()]);
                    setTagInput("");
                  }
                }}
              />
            </div>
          </div>

          {t.sentimentScore !== null && (
            <div className="tp-card p-5">
              <div className="text-xs uppercase tracking-wider mb-4" style={{ color: "var(--subtle)" }}>Sentiment score</div>
              <SentGauge score={sentScore} />
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div style={{ color: "var(--subtle)" }}>{label}</div>
      <div className="mt-0.5 font-medium capitalize" style={{ color: "var(--foreground)" }}>{value}</div>
    </div>
  );
}

function SentGauge({ score }: { score: number }) {
  const pct = ((score + 1) / 2) * 100;
  const color = score > 0.3 ? "var(--success)" : score < -0.2 ? "var(--danger)" : "var(--warning)";
  return (
    <div>
      <div className="flex justify-between text-xs mb-2" style={{ color: "var(--subtle)" }}>
        <span>−1</span><span>0</span><span>+1</span>
      </div>
      <div className="relative h-2 rounded-full" style={{ background: "var(--border)" }}>
        <div className="absolute h-full" style={{ width: `${pct}%`, background: color, borderRadius: 999 }} />
      </div>
      <div className="mt-3 flex items-baseline gap-2">
        <span className="text-3xl font-semibold" style={{ color }}>{score.toFixed(2)}</span>
        <span className="text-sm capitalize" style={{ color: "var(--muted-foreground)" }}>
          {score > 0.3 ? "positive" : score < -0.2 ? "negative" : "neutral"}
        </span>
      </div>
    </div>
  );
}
