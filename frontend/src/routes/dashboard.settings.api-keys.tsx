import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Plus, Copy, AlertTriangle, Trash2 } from "lucide-react";

export const Route = createFileRoute("/dashboard/settings/api-keys")({
  head: () => ({ meta: [{ title: "API keys — TrustPanel" }] }),
  component: ApiKeys,
});

const keys = [
  { name: "Production server", preview: "tp_live_8a3f", lastUsed: "2 min ago", created: "Jan 12, 2025", role: "Read + Write" },
  { name: "Marketing site (read-only)", preview: "tp_live_2c91", lastUsed: "1 day ago", created: "Feb 04, 2025", role: "Read only" },
  { name: "Zapier integration", preview: "tp_live_e44b", lastUsed: "3 days ago", created: "Mar 21, 2025", role: "Read + Write" },
];

function ApiKeys() {
  const [revealed, setRevealed] = useState(false);
  return (
    <DashboardLayout
      title="API keys"
      action={
        <button onClick={() => setRevealed(true)} className="tp-btn tp-btn-primary">
          <Plus size={14} /> Create key
        </button>
      }
    >
      {revealed && (
        <div className="tp-card p-5 mb-6" style={{ background: "rgba(251,191,36,0.06)", borderColor: "var(--warning)" }}>
          <div className="flex items-center gap-2 mb-3">
            <AlertTriangle size={14} style={{ color: "var(--warning)" }} />
            <span className="font-semibold" style={{ color: "var(--warning)" }}>
              This is the only time your key will be shown
            </span>
          </div>
          <div className="font-mono text-sm tp-card p-3 flex items-center justify-between" style={{ background: "var(--surface)" }}>
            <span style={{ color: "var(--primary-light)" }}>tp_live_b9a4e7f1c8d2e0a5b3f6h9k1m4n7q2r8</span>
            <button className="tp-btn tp-btn-ghost" style={{ padding: "4px 8px" }}>
              <Copy size={12} /> Copy
            </button>
          </div>
          <div className="flex gap-2 mt-4">
            <button onClick={() => setRevealed(false)} className="tp-btn tp-btn-primary">
              I've saved it
            </button>
          </div>
        </div>
      )}

      <div className="tp-card overflow-hidden mb-8">
        <table className="w-full text-sm">
          <thead style={{ background: "var(--surface)" }}>
            <tr>
              {["Name", "Key", "Permissions", "Last used", "Created", ""].map((h) => (
                <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {keys.map((k) => (
              <tr key={k.preview} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3 font-medium">{k.name}</td>
                <td className="px-5 py-3 font-mono text-xs" style={{ color: "var(--muted-foreground)" }}>
                  {k.preview}••••••••••••
                </td>
                <td className="px-5 py-3">
                  <Pill tone={k.role.includes("Write") ? "primary" : "neutral"}>{k.role}</Pill>
                </td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>{k.lastUsed}</td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>{k.created}</td>
                <td className="px-5 py-3 text-right">
                  <button className="text-xs" style={{ color: "var(--danger)" }}>
                    <Trash2 size={11} className="inline" /> Revoke
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="tp-card p-6">
        <div className="font-semibold mb-4">Quick API reference</div>
        <div className="space-y-3 font-mono text-xs">
          <CodeBlock label="Base URL" code="https://api.trustpanel.com/v1" />
          <CodeBlock label="List testimonials" code={`curl https://api.trustpanel.com/v1/testimonials \\
  -H "Authorization: Bearer tp_live_..."`} />
          <CodeBlock label="Create testimonial" code={`curl -X POST https://api.trustpanel.com/v1/testimonials \\
  -H "Authorization: Bearer tp_live_..." \\
  -d '{ "name": "Sarah K.", "text": "...", "rating": 5 }'`} />
          <CodeBlock label="Fetch widget data" code={`curl https://api.trustpanel.com/v1/widgets/wgt_a8f3`} />
        </div>
        <a href="#" className="text-sm mt-5 inline-block" style={{ color: "var(--primary-light)" }}>
          Read the full API docs →
        </a>
      </div>
    </DashboardLayout>
  );
}

function CodeBlock({ label, code }: { label: string; code: string }) {
  return (
    <div>
      <div className="text-xs mb-1.5" style={{ color: "var(--subtle)", fontFamily: "var(--font-sans)" }}>
        {label}
      </div>
      <pre className="tp-card p-3 whitespace-pre-wrap" style={{ background: "var(--surface)", color: "var(--primary-light)" }}>
{code}
      </pre>
    </div>
  );
}
