import { createFileRoute } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Sparkles, RefreshCw } from "lucide-react";
import { LineChart, Line, ResponsiveContainer, XAxis, YAxis, CartesianGrid, Tooltip } from "recharts";
import { sentimentTrend, ratingDistribution } from "@/lib/mock-data";

export const Route = createFileRoute("/dashboard/insights")({
  head: () => ({ meta: [{ title: "AI Insights — TrustPanel" }] }),
  component: Insights,
});

const loves = [
  { theme: "Speed & Performance", count: 47, score: 0.92, quote: "Setup took 11 minutes. Eleven." },
  { theme: "Customer Support", count: 38, score: 0.88, quote: "They responded in under an hour with an actual fix." },
  { theme: "Ease of Use", count: 31, score: 0.85, quote: "My non-technical clients can use this without my help." },
  { theme: "Branding & Design", count: 22, score: 0.81, quote: "Finally doesn't look like a third-party plugin." },
];

const concerns = [
  { theme: "Mobile widget options", count: 8, score: -0.32, quote: "The mobile layout could use more density controls." },
  { theme: "API documentation", count: 5, score: -0.48, quote: "Docs are a bit thin around webhooks." },
];

const phrases = [
  ["easy to use", 142],
  ["saved us time", 98],
  ["highly recommend", 87],
  ["beautiful design", 76],
  ["set up in minutes", 64],
  ["just works", 52],
];

function Insights() {
  return (
    <DashboardLayout
      title="Your testimonial insights"
      action={
        <>
          <span className="text-xs" style={{ color: "var(--subtle)" }}>
            Last generated 2h ago
          </span>
          <button className="tp-btn tp-btn-ghost">
            <RefreshCw size={14} /> Regenerate
          </button>
        </>
      }
    >
      <Section title="What customers love" tone="success">
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-4">
          {loves.map((l) => (
            <ThemeCard key={l.theme} theme={l.theme} count={l.count} score={l.score} quote={l.quote} positive />
          ))}
        </div>
      </Section>

      <Section title="Common language">
        <div className="grid md:grid-cols-2 gap-4">
          <div className="tp-card p-6 flex flex-wrap gap-2 content-start" style={{ minHeight: 220 }}>
            {phrases.map(([w, n], i) => (
              <span
                key={w as string}
                style={{
                  fontSize: 12 + (n as number) / 14,
                  color: i % 2 === 0 ? "var(--primary-light)" : "var(--info)",
                  padding: "4px 10px",
                  borderRadius: 999,
                  background: "var(--surface)",
                }}
              >
                {w}
              </span>
            ))}
          </div>
          <div className="tp-card p-6">
            <div className="text-xs uppercase tracking-wider mb-4" style={{ color: "var(--subtle)" }}>
              Top phrases
            </div>
            <div className="space-y-3">
              {phrases.map(([w, n]) => (
                <div key={w as string}>
                  <div className="flex justify-between text-sm mb-1">
                    <span>{w}</span>
                    <span style={{ color: "var(--subtle)" }}>{n}</span>
                  </div>
                  <div className="h-1.5 rounded-full" style={{ background: "var(--border)" }}>
                    <div className="h-full rounded-full" style={{ background: "var(--primary)", width: `${((n as number) / 142) * 100}%` }} />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </Section>

      <Section title="Areas to address" tone="warning">
        <div className="grid md:grid-cols-2 gap-4">
          {concerns.map((c) => (
            <ThemeCard key={c.theme} theme={c.theme} count={c.count} score={c.score} quote={c.quote} />
          ))}
        </div>
      </Section>

      <Section title="Rating distribution">
        <div className="tp-card p-6 space-y-3">
          {ratingDistribution.map((r) => (
            <div key={r.stars} className="flex items-center gap-4">
              <span className="w-10 text-sm font-medium" style={{ color: "var(--warning)" }}>
                {r.stars}
              </span>
              <div className="flex-1 h-3 rounded-full" style={{ background: "var(--border)" }}>
                <div className="h-full rounded-full" style={{ background: "var(--primary)", width: `${r.pct}%` }} />
              </div>
              <span className="w-12 text-right text-sm" style={{ color: "var(--muted-foreground)" }}>
                {r.pct}%
              </span>
            </div>
          ))}
        </div>
      </Section>

      <Section title="Sentiment over time">
        <div className="tp-card p-5">
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={sentimentTrend}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="month" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis domain={[-1, 1]} stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: 8 }} />
              <Line type="monotone" dataKey="score" stroke="var(--primary)" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </Section>
    </DashboardLayout>
  );
}

function Section({ title, tone, children }: { title: string; tone?: "success" | "warning"; children: React.ReactNode }) {
  return (
    <div className="mb-10">
      <div className="flex items-center gap-2 mb-4">
        <Sparkles size={14} style={{ color: tone === "warning" ? "var(--warning)" : "var(--primary-light)" }} />
        <h2 className="text-lg font-semibold">{title}</h2>
      </div>
      {children}
    </div>
  );
}

function ThemeCard({ theme, count, score, quote, positive }: { theme: string; count: number; score: number; quote: string; positive?: boolean }) {
  return (
    <div className="tp-card p-5">
      <div className="flex items-start justify-between mb-3">
        <div>
          <div className="font-semibold">{theme}</div>
          <div className="text-xs mt-0.5" style={{ color: "var(--subtle)" }}>
            {count} mentions
          </div>
        </div>
        <Pill tone={positive ? "success" : "danger"}>
          {score > 0 ? "+" : ""}
          {score.toFixed(2)}
        </Pill>
      </div>
      <p className="text-sm italic" style={{ color: "var(--muted-foreground)" }}>
        "{quote}"
      </p>
      <div className="mt-4 h-1 rounded-full" style={{ background: "var(--border)" }}>
        <div
          className="h-full rounded-full"
          style={{
            background: positive ? "var(--success)" : "var(--danger)",
            width: `${Math.abs(score) * 100}%`,
          }}
        />
      </div>
    </div>
  );
}
