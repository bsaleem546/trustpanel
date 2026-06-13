import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Sparkles, RefreshCw, Loader } from "lucide-react";
import { aiApi } from "@/lib/api/ai";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/insights")({
  head: () => ({ meta: [{ title: "AI Insights — TrustPanel" }] }),
  component: Insights,
});

function Insights() {
  useRequireAuth();
  const { data: me } = useMe();

  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ["ai-insights", me?.workspaceId],
    queryFn: () => aiApi.insights(me!.workspaceId!),
    enabled: !!me?.workspaceId,
    staleTime: 5 * 60_000,
    refetchInterval: (query) => {
      // Poll every 10s while still generating
      const d = query.state.data as { generating?: boolean } | undefined;
      return d?.generating ? 10_000 : false;
    },
  });

  const generating = (data as { generating?: boolean } | undefined)?.generating;
  const insights = generating ? null : (data as Record<string, unknown> | undefined);

  return (
    <DashboardLayout
      title="AI Insights"
      action={
        <button className="tp-btn tp-btn-ghost" onClick={() => refetch()} disabled={isFetching}>
          <RefreshCw size={14} className={isFetching ? "animate-spin" : ""} /> Refresh
        </button>
      }
    >
      {isLoading && (
        <div className="flex items-center gap-3 text-sm" style={{ color: "var(--subtle)" }}>
          <Loader size={16} className="animate-spin" /> Loading insights…
        </div>
      )}

      {generating && !isLoading && (
        <div className="tp-card p-10 text-center">
          <Sparkles size={32} className="mx-auto mb-4" style={{ color: "var(--primary-light)" }} />
          <div className="font-semibold text-lg mb-2">Generating insights…</div>
          <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>
            We're analysing your testimonials with AI. This usually takes a minute.
          </p>
          <div className="mt-4 flex items-center justify-center gap-2 text-xs" style={{ color: "var(--subtle)" }}>
            <Loader size={12} className="animate-spin" /> Checking every 10 seconds…
          </div>
        </div>
      )}

      {insights && (
        <div className="space-y-8">
          {insights.summary && (
            <Section title="Summary">
              <div className="tp-card p-6 text-sm leading-relaxed" style={{ color: "var(--muted-foreground)" }}>
                {insights.summary as string}
              </div>
            </Section>
          )}

          {Array.isArray(insights.topThemes) && insights.topThemes.length > 0 && (
            <Section title="Top themes">
              <div className="flex flex-wrap gap-2">
                {(insights.topThemes as string[]).map((t) => (
                  <Pill key={t} tone="primary">{t}</Pill>
                ))}
              </div>
            </Section>
          )}

          {insights.sentimentSummary && (
            <Section title="Sentiment">
              <div className="tp-card p-6 text-sm leading-relaxed" style={{ color: "var(--muted-foreground)" }}>
                {insights.sentimentSummary as string}
              </div>
            </Section>
          )}

          {Array.isArray(insights.recommendations) && insights.recommendations.length > 0 && (
            <Section title="Recommendations">
              <div className="tp-card p-6">
                <ul className="space-y-3">
                  {(insights.recommendations as string[]).map((r, i) => (
                    <li key={i} className="flex gap-3 text-sm">
                      <span className="shrink-0 w-5 h-5 rounded-full flex items-center justify-center text-xs font-medium"
                        style={{ background: "var(--primary-soft)", color: "var(--primary-light)" }}>
                        {i + 1}
                      </span>
                      <span style={{ color: "var(--muted-foreground)" }}>{r}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </Section>
          )}

          {/* Render any other top-level keys as raw JSON for future-proofing */}
          {Object.entries(insights)
            .filter(([k]) => !["summary", "topThemes", "sentimentSummary", "recommendations"].includes(k))
            .map(([k, v]) => (
              <Section key={k} title={k.replace(/([A-Z])/g, " $1").trim()}>
                <div className="tp-card p-4 font-mono text-xs overflow-auto" style={{ color: "var(--muted-foreground)" }}>
                  {JSON.stringify(v, null, 2)}
                </div>
              </Section>
            ))}
        </div>
      )}

      {!isLoading && !generating && !insights && (
        <div className="tp-card p-10 text-center">
          <Sparkles size={32} className="mx-auto mb-4" style={{ color: "var(--subtle)" }} />
          <div className="font-semibold mb-2">No insights yet</div>
          <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>
            Insights are generated automatically as testimonials come in.
          </p>
        </div>
      )}
    </DashboardLayout>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="flex items-center gap-2 mb-3">
        <Sparkles size={14} style={{ color: "var(--primary-light)" }} />
        <h2 className="text-lg font-semibold">{title}</h2>
      </div>
      {children}
    </div>
  );
}
