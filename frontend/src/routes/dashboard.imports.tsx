import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Pill, Stars } from "@/components/Stars";
import { Plus, Check } from "lucide-react";

export const Route = createFileRoute("/dashboard/imports")({
  head: () => ({ meta: [{ title: "Imports — TrustPanel" }] }),
  component: Imports,
});

const sources = [
  { name: "Google Business Profile", icon: "🔵", connected: true, synced: "2 hours ago", count: 47 },
  { name: "Twitter / X", icon: "✕", connected: true, synced: "1 day ago", count: 23 },
  { name: "G2", icon: "🟧", connected: false, synced: null, count: 0 },
  { name: "Trustpilot", icon: "⭐", connected: true, synced: "3 days ago", count: 18 },
  { name: "Capterra", icon: "🟦", connected: false, synced: null, count: 0 },
];

const queue = [
  { source: "Google", name: "Liam Carter", excerpt: "Quick service, friendly team.", rating: 5, relevance: 0.92 },
  { source: "Trustpilot", name: "Hannah Ng", excerpt: "Good product but pricing tiers are confusing.", rating: 3, relevance: 0.61 },
  { source: "Google", name: "Devon Rios", excerpt: "Saved our launch.", rating: 5, relevance: 0.88 },
];

function Imports() {
  const [open, setOpen] = useState(false);
  return (
    <DashboardLayout
      title="Import sources"
      action={
        <button className="tp-btn tp-btn-primary" onClick={() => setOpen(true)}>
          <Plus size={14} /> Add source
        </button>
      }
    >
      <div className="grid md:grid-cols-2 gap-4 mb-8">
        {sources.map((s) => (
          <div key={s.name} className="tp-card p-5 flex items-center gap-4">
            <div className="w-12 h-12 rounded-lg flex items-center justify-center text-2xl" style={{ background: "var(--surface)" }}>
              {s.icon}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <span className="font-medium">{s.name}</span>
                <Pill tone={s.connected ? "success" : "neutral"}>{s.connected ? "Connected" : "Not connected"}</Pill>
              </div>
              <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>
                {s.connected ? `Last synced ${s.synced} · ${s.count} testimonials` : "Connect to start importing"}
              </div>
            </div>
            <div className="flex gap-2">
              {s.connected ? (
                <>
                  <button className="tp-btn tp-btn-ghost" style={{ fontSize: 12 }}>Sync now</button>
                  <button className="tp-btn tp-btn-ghost" style={{ fontSize: 12, color: "var(--danger)" }}>Disconnect</button>
                </>
              ) : (
                <button className="tp-btn tp-btn-primary" style={{ fontSize: 12 }}>Connect</button>
              )}
            </div>
          </div>
        ))}
      </div>

      <div className="tp-card overflow-hidden">
        <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
          <div className="font-semibold">Imported queue</div>
          <div className="text-xs mt-0.5" style={{ color: "var(--subtle)" }}>
            Review and approve recently imported items
          </div>
        </div>
        <table className="w-full text-sm">
          <thead style={{ background: "var(--surface)" }}>
            <tr>
              {["Source", "Submitter", "Excerpt", "Rating", "AI relevance", ""].map((h) => (
                <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {queue.map((q, i) => (
              <tr key={i} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3"><Pill tone="info">{q.source}</Pill></td>
                <td className="px-5 py-3 flex items-center gap-2">
                  <Avatar name={q.name} color="#a594f9" size={28} />
                  {q.name}
                </td>
                <td className="px-5 py-3 max-w-xs truncate" style={{ color: "var(--muted-foreground)" }}>
                  {q.excerpt}
                </td>
                <td className="px-5 py-3"><Stars value={q.rating} /></td>
                <td className="px-5 py-3" style={{ color: q.relevance > 0.7 ? "var(--success)" : "var(--warning)" }}>
                  {(q.relevance * 100).toFixed(0)}%
                </td>
                <td className="px-5 py-3 text-right">
                  <button className="tp-btn tp-btn-success" style={{ padding: "4px 10px", fontSize: 12 }}>Keep</button>
                  <button className="tp-btn tp-btn-ghost ml-1" style={{ padding: "4px 10px", fontSize: 12 }}>Discard</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4" style={{ background: "rgba(0,0,0,0.6)" }}>
          <div className="tp-card p-6 w-full max-w-2xl">
            <div className="flex justify-between items-center mb-5">
              <div className="font-semibold text-lg">Add an import source</div>
              <button className="text-sm" onClick={() => setOpen(false)} style={{ color: "var(--subtle)" }}>
                Close
              </button>
            </div>
            <div className="grid grid-cols-3 gap-3">
              {sources.map((s) => (
                <button key={s.name} className="tp-card p-4 text-center hover:border-primary" style={{ background: "var(--surface)" }}>
                  <div className="text-3xl mb-2">{s.icon}</div>
                  <div className="text-sm font-medium">{s.name}</div>
                  {s.connected && (
                    <div className="text-xs mt-1 flex justify-center items-center gap-1" style={{ color: "var(--success)" }}>
                      <Check size={11} /> Connected
                    </div>
                  )}
                </button>
              ))}
            </div>
            <div className="mt-5 tp-card p-4" style={{ background: "var(--surface)" }}>
              <div className="text-sm font-medium mb-2">Selected: Google Business Profile</div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Location ID
              </label>
              <input className="tp-input font-mono text-xs" placeholder="accounts/123456/locations/789" />
              <button className="tp-btn tp-btn-primary mt-3">Connect</button>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}
