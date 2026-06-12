import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Upload, Calendar, Send } from "lucide-react";

export const Route = createFileRoute("/dashboard/requests")({
  head: () => ({ meta: [{ title: "Send requests — TrustPanel" }] }),
  component: Requests,
});

const sent = [
  { name: "Olivia Park", email: "olivia@brixton.co", form: "Homepage", sent: "2h ago", status: "Submitted" },
  { name: "Noah Sterling", email: "noah@vellum.io", form: "Homepage", sent: "5h ago", status: "Opened" },
  { name: "Mia Hernandez", email: "mia@latitude.gg", form: "Post-purchase", sent: "1d ago", status: "Clicked" },
  { name: "Ethan Park", email: "ethan@morrowloop.com", form: "Webinar", sent: "2d ago", status: "Sent" },
  { name: "Charlotte Wong", email: "charlotte@axisflow.io", form: "Homepage", sent: "3d ago", status: "Submitted" },
];

const statusTone: Record<string, "neutral" | "info" | "warning" | "success"> = {
  Sent: "neutral",
  Opened: "info",
  Clicked: "warning",
  Submitted: "success",
};

function Requests() {
  const [tab, setTab] = useState<"single" | "bulk" | "history">("single");
  return (
    <DashboardLayout title="Send requests">
      <div className="flex gap-1 mb-6 tp-card p-1 inline-flex">
        {([
          ["single", "Single request"],
          ["bulk", "Bulk send"],
          ["history", "Sent history"],
        ] as const).map(([k, l]) => (
          <button
            key={k}
            onClick={() => setTab(k)}
            className="px-4 py-2 rounded-md text-sm font-medium"
            style={{
              background: tab === k ? "var(--primary)" : "transparent",
              color: tab === k ? "white" : "var(--muted-foreground)",
            }}
          >
            {l}
          </button>
        ))}
      </div>

      {tab === "single" && (
        <div className="tp-card p-6 max-w-2xl space-y-4">
          <div className="grid md:grid-cols-2 gap-4">
            <Field label="Recipient name">
              <input className="tp-input" placeholder="Sarah Kowalski" />
            </Field>
            <Field label="Email">
              <input className="tp-input" placeholder="sarah@northwind.studios" />
            </Field>
          </div>
          <Field label="Form">
            <select className="tp-input">
              <option>Homepage testimonial</option>
              <option>Post-purchase NPS</option>
            </select>
          </Field>
          <Field label="Email template">
            <select className="tp-input">
              <option>Default — friendly request</option>
              <option>Quick & casual</option>
              <option>Formal customer letter</option>
            </select>
          </Field>
          <Field label="Custom message">
            <textarea className="tp-input" rows={4} defaultValue="Hi Sarah — would love to hear how things have been going with the rebrand. Mind sharing a quick note?" />
          </Field>
          <button className="tp-btn tp-btn-primary">
            <Send size={14} /> Send request
          </button>
        </div>
      )}

      {tab === "bulk" && (
        <div className="tp-card p-6 max-w-2xl space-y-4">
          <div className="border-2 border-dashed rounded-lg p-8 text-center" style={{ borderColor: "var(--border-hover)" }}>
            <Upload size={20} className="mx-auto mb-2" style={{ color: "var(--primary-light)" }} />
            <div className="text-sm">Upload CSV with columns: name, email</div>
            <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>
              or paste emails below
            </div>
          </div>
          <textarea className="tp-input font-mono text-xs" rows={5} placeholder="sarah@…\nmarco@…" />
          <div className="grid md:grid-cols-2 gap-4">
            <Field label="Form">
              <select className="tp-input">
                <option>Homepage testimonial</option>
              </select>
            </Field>
            <Field label="Template">
              <select className="tp-input">
                <option>Default — friendly request</option>
              </select>
            </Field>
          </div>
          <Field label="Schedule">
            <div className="flex gap-2">
              <button className="tp-btn tp-btn-primary">Send now</button>
              <button className="tp-btn tp-btn-ghost">
                <Calendar size={14} /> Schedule
              </button>
            </div>
          </Field>
          <div className="text-sm tp-card p-3" style={{ background: "var(--primary-soft)", color: "var(--primary-light)", borderColor: "var(--primary)" }}>
            Ready to send to <strong>47 recipients</strong>.
          </div>
        </div>
      )}

      {tab === "history" && (
        <div className="tp-card overflow-hidden">
          <table className="w-full text-sm">
            <thead style={{ background: "var(--surface)" }}>
              <tr>
                {["Recipient", "Form", "Sent", "Status", ""].map((h) => (
                  <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {sent.map((s) => (
                <tr key={s.email} style={{ borderTop: "1px solid var(--border)" }}>
                  <td className="px-5 py-3">
                    <div className="font-medium">{s.name}</div>
                    <div className="text-xs" style={{ color: "var(--subtle)" }}>
                      {s.email}
                    </div>
                  </td>
                  <td className="px-5 py-3">{s.form}</td>
                  <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                    {s.sent}
                  </td>
                  <td className="px-5 py-3">
                    <Pill tone={statusTone[s.status]}>{s.status}</Pill>
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button className="text-xs" style={{ color: "var(--primary-light)" }}>
                      Resend
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </DashboardLayout>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
        {label}
      </label>
      {children}
    </div>
  );
}
