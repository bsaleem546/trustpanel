import { createFileRoute } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { LineChart, Line, BarChart, Bar, ResponsiveContainer, XAxis, YAxis, Tooltip, CartesianGrid, PieChart, Pie, Cell } from "recharts";
import { impressionsData, submissionsByForm, sentimentTrend } from "@/lib/mock-data";
import { Pill } from "@/components/Stars";

export const Route = createFileRoute("/dashboard/analytics")({
  head: () => ({ meta: [{ title: "Analytics — TrustPanel" }] }),
  component: Analytics,
});

const metrics = [
  { l: "Widget impressions", v: "12,840", d: "+24.1%" },
  { l: "Widget clicks", v: "1,284", d: "+18.4%" },
  { l: "Click-through rate", v: "10.0%", d: "+1.2%" },
  { l: "Forms submitted", v: "47", d: "+6 this week" },
];

const topTestimonials = [
  { rank: 1, name: "Sarah Kowalski", clicks: 412, impressions: 4820 },
  { rank: 2, name: "Marco Bertelli", clicks: 318, impressions: 3940 },
  { rank: 3, name: "Daniel Okafor", clicks: 261, impressions: 3120 },
  { rank: 4, name: "Priya Raman", clicks: 198, impressions: 2680 },
];

const sources = [
  { l: "Direct", v: 5240 },
  { l: "Google", v: 3120 },
  { l: "Twitter", v: 1840 },
  { l: "Other", v: 2640 },
];

const devices = [
  { name: "Mobile", value: 56 },
  { name: "Desktop", value: 36 },
  { name: "Tablet", value: 8 },
];

function Analytics() {
  return (
    <DashboardLayout
      title="Analytics"
      action={
        <div className="flex gap-1 tp-card p-1">
          {["7d", "30d", "90d", "Custom"].map((r, i) => (
            <button
              key={r}
              className="px-3 py-1 rounded-md text-xs"
              style={{ background: i === 1 ? "var(--primary)" : "transparent", color: i === 1 ? "white" : "var(--muted-foreground)" }}
            >
              {r}
            </button>
          ))}
        </div>
      }
    >
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {metrics.map((m) => (
          <div key={m.l} className="tp-card p-5">
            <div className="text-xs" style={{ color: "var(--subtle)" }}>
              {m.l}
            </div>
            <div className="text-3xl font-semibold mt-2">{m.v}</div>
            <Pill tone="success">{m.d}</Pill>
          </div>
        ))}
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <ChartCard title="Impressions over time">
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={impressionsData}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="day" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: 8, color: "var(--foreground)" }} />
              <Line type="monotone" dataKey="impressions" stroke="var(--primary)" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Submissions per form">
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={submissionsByForm}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="name" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: 8 }} />
              <Bar dataKey="submissions" fill="var(--primary)" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <div className="tp-card">
          <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="font-semibold">Top testimonials by engagement</div>
          </div>
          <table className="w-full text-sm">
            <tbody>
              {topTestimonials.map((t) => (
                <tr key={t.rank} style={{ borderTop: "1px solid var(--border)" }}>
                  <td className="px-5 py-3 w-10" style={{ color: "var(--subtle)" }}>
                    #{t.rank}
                  </td>
                  <td className="px-5 py-3 font-medium">{t.name}</td>
                  <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                    {t.clicks} clicks
                  </td>
                  <td className="px-5 py-3 text-right" style={{ color: "var(--subtle)" }}>
                    {t.impressions.toLocaleString()} views
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <ChartCard title="Traffic sources">
          <div className="space-y-3">
            {sources.map((s) => {
              const max = Math.max(...sources.map((x) => x.v));
              return (
                <div key={s.l}>
                  <div className="flex justify-between text-xs mb-1.5">
                    <span>{s.l}</span>
                    <span style={{ color: "var(--subtle)" }}>{s.v.toLocaleString()}</span>
                  </div>
                  <div className="h-2 rounded-full" style={{ background: "var(--border)" }}>
                    <div className="h-full rounded-full" style={{ background: "var(--primary)", width: `${(s.v / max) * 100}%` }} />
                  </div>
                </div>
              );
            })}
          </div>
        </ChartCard>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <ChartCard title="Geo distribution">
          <div className="h-[220px] rounded-lg flex items-center justify-center" style={{ background: "var(--surface)", border: "1px dashed var(--border)" }}>
            <div className="text-center" style={{ color: "var(--subtle)" }}>
              <div className="text-3xl mb-2">🌎</div>
              <div className="text-xs">World heatmap · top: US, GB, DE, FR, IN</div>
            </div>
          </div>
        </ChartCard>

        <ChartCard title="Devices">
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie data={devices} dataKey="value" innerRadius={50} outerRadius={80} paddingAngle={2}>
                {devices.map((_, i) => (
                  <Cell key={i} fill={["var(--primary)", "var(--primary-light)", "var(--info)"][i]} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: 8 }} />
            </PieChart>
          </ResponsiveContainer>
          <div className="flex justify-center gap-4 text-xs mt-2">
            {devices.map((d, i) => (
              <div key={d.name} className="flex items-center gap-1.5">
                <span className="w-2 h-2 rounded-full" style={{ background: ["var(--primary)", "var(--primary-light)", "var(--info)"][i] }} />
                {d.name} {d.value}%
              </div>
            ))}
          </div>
        </ChartCard>
      </div>

      <ChartCard title="Sentiment over time">
        <ResponsiveContainer width="100%" height={220}>
          <LineChart data={sentimentTrend}>
            <CartesianGrid stroke="var(--border)" vertical={false} />
            <XAxis dataKey="month" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
            <YAxis domain={[-1, 1]} stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
            <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: 8 }} />
            <Line type="monotone" dataKey="score" stroke="var(--success)" strokeWidth={2} dot={false} />
          </LineChart>
        </ResponsiveContainer>
      </ChartCard>
    </DashboardLayout>
  );
}

function ChartCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="tp-card p-5">
      <div className="font-semibold mb-4">{title}</div>
      {children}
    </div>
  );
}
