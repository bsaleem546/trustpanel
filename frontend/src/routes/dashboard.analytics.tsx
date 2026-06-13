import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import {
  LineChart, Line, BarChart, Bar, ResponsiveContainer,
  XAxis, YAxis, Tooltip, CartesianGrid, PieChart, Pie, Cell,
} from "recharts";
import { analyticsApi, type DailyCount, type StringBucket } from "@/lib/api/analytics";
import { useMe, useRequireAuth } from "@/lib/auth";
import { Download } from "lucide-react";

export const Route = createFileRoute("/dashboard/analytics")({
  head: () => ({ meta: [{ title: "Analytics — TrustPanel" }] }),
  component: Analytics,
});

const RANGE_OPTIONS = [
  { label: "7d", days: 7 },
  { label: "30d", days: 30 },
  { label: "90d", days: 90 },
];

const CHART_STYLE = {
  background: "var(--card)",
  border: "1px solid var(--border)",
  borderRadius: 8,
  color: "var(--foreground)",
};

function fmt(date: string) {
  return new Date(date).toLocaleDateString(undefined, { month: "short", day: "numeric" });
}

function Analytics() {
  useRequireAuth();
  const { data: me } = useMe();
  const [rangeIdx, setRangeIdx] = useState(1);
  const daysBack = RANGE_OPTIONS[rangeIdx].days;

  const { data, isLoading } = useQuery({
    queryKey: ["analytics", me?.workspaceId, daysBack],
    queryFn: () => analyticsApi.dashboard(me!.workspaceId!, daysBack),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const submissions = (data?.submissionsOverTime ?? []).map((d: DailyCount) => ({
    day: fmt(d.date),
    submissions: d.count,
  }));
  const impressions = (data?.impressionsOverTime ?? []).map((d: DailyCount) => ({
    day: fmt(d.date),
    impressions: d.count,
  }));
  const devices = (data?.topDevices ?? []).map((d: StringBucket, i: number) => ({
    name: d.key,
    value: Number(d.count),
    color: ["var(--primary)", "var(--primary-light)", "var(--info)", "var(--warning)", "var(--success)"][i % 5],
  }));
  const countries = data?.topCountries ?? [];
  const maxCountry = Math.max(...countries.map((c) => Number(c.count)), 1);

  const csvUrl = me?.workspaceId
    ? analyticsApi.exportCsvUrl(me.workspaceId, daysBack)
    : "#";

  return (
    <DashboardLayout
      title="Analytics"
      action={
        <div className="flex gap-2 items-center">
          <a href={csvUrl} download className="tp-btn tp-btn-ghost text-xs" style={{ padding: "6px 10px" }}>
            <Download size={12} /> Export CSV
          </a>
          <div className="flex gap-1 tp-card p-1">
            {RANGE_OPTIONS.map((r, i) => (
              <button
                key={r.label}
                onClick={() => setRangeIdx(i)}
                className="px-3 py-1 rounded-md text-xs"
                style={{
                  background: i === rangeIdx ? "var(--primary)" : "transparent",
                  color: i === rangeIdx ? "white" : "var(--muted-foreground)",
                }}
              >
                {r.label}
              </button>
            ))}
          </div>
        </div>
      }
    >
      {isLoading && <div className="text-sm mb-4" style={{ color: "var(--subtle)" }}>Loading analytics…</div>}

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard label="Total approved" value={data ? String(data.totalApproved) : "—"} />
        <StatCard label="Pending review" value={data ? String(data.totalPending) : "—"} />
        <StatCard label="Widget impressions" value={data ? String(data.impressionsOverTime.reduce((s, d) => s + Number(d.count), 0).toLocaleString()) : "—"} />
        <StatCard label="Submissions" value={data ? String(data.submissionsOverTime.reduce((s, d) => s + Number(d.count), 0).toLocaleString()) : "—"} />
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <ChartCard title="Submissions over time">
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={submissions}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="day" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={CHART_STYLE} />
              <Line type="monotone" dataKey="submissions" stroke="var(--primary)" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Widget impressions over time">
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={impressions}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="day" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={CHART_STYLE} />
              <Line type="monotone" dataKey="impressions" stroke="var(--primary-light)" strokeWidth={2} dot={false} />
            </LineChart>
          </ResponsiveContainer>
        </ChartCard>
      </div>

      <div className="grid lg:grid-cols-2 gap-6 mb-6">
        <ChartCard title="Rating distribution">
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={data?.ratingDistribution.map((r) => ({ rating: `${r.rating}★`, count: r.count })) ?? []}>
              <CartesianGrid stroke="var(--border)" vertical={false} />
              <XAxis dataKey="rating" stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <YAxis stroke="var(--subtle)" fontSize={11} tickLine={false} axisLine={false} />
              <Tooltip contentStyle={CHART_STYLE} />
              <Bar dataKey="count" fill="var(--primary)" radius={[6, 6, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </ChartCard>

        <ChartCard title="Devices">
          {devices.length > 0 ? (
            <>
              <ResponsiveContainer width="100%" height={180}>
                <PieChart>
                  <Pie data={devices} dataKey="value" innerRadius={50} outerRadius={80} paddingAngle={2}>
                    {devices.map((d, i) => <Cell key={i} fill={d.color} />)}
                  </Pie>
                  <Tooltip contentStyle={CHART_STYLE} />
                </PieChart>
              </ResponsiveContainer>
              <div className="flex justify-center flex-wrap gap-3 text-xs mt-2">
                {devices.map((d) => (
                  <div key={d.name} className="flex items-center gap-1.5">
                    <span className="w-2 h-2 rounded-full" style={{ background: d.color }} />
                    {d.name} ({d.value.toLocaleString()})
                  </div>
                ))}
              </div>
            </>
          ) : (
            <div className="h-40 flex items-center justify-center text-sm" style={{ color: "var(--subtle)" }}>
              No device data yet
            </div>
          )}
        </ChartCard>
      </div>

      <ChartCard title="Top countries">
        {countries.length > 0 ? (
          <div className="space-y-3">
            {countries.map((c) => (
              <div key={c.key}>
                <div className="flex justify-between text-xs mb-1.5">
                  <span>{c.key}</span>
                  <span style={{ color: "var(--subtle)" }}>{Number(c.count).toLocaleString()}</span>
                </div>
                <div className="h-2 rounded-full" style={{ background: "var(--border)" }}>
                  <div className="h-full rounded-full" style={{ background: "var(--primary)", width: `${(Number(c.count) / maxCountry) * 100}%` }} />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="h-20 flex items-center justify-center text-sm" style={{ color: "var(--subtle)" }}>
            No geo data yet
          </div>
        )}
      </ChartCard>
    </DashboardLayout>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="tp-card p-5">
      <div className="text-xs mb-2" style={{ color: "var(--subtle)" }}>{label}</div>
      <div className="text-3xl font-semibold">{value}</div>
    </div>
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
