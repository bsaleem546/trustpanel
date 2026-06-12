import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Check, Upload } from "lucide-react";

export const Route = createFileRoute("/dashboard/settings/workspace")({
  head: () => ({ meta: [{ title: "Workspace settings — TrustPanel" }] }),
  component: WorkspaceSettings,
});

const tabs = ["General", "Branding", "Domain", "Notifications"] as const;

function WorkspaceSettings() {
  const [tab, setTab] = useState<(typeof tabs)[number]>("General");
  return (
    <DashboardLayout title="Workspace settings">
      <div className="flex gap-1 mb-6 border-b" style={{ borderColor: "var(--border)" }}>
        {tabs.map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className="px-4 py-2.5 text-sm font-medium relative"
            style={{ color: tab === t ? "var(--foreground)" : "var(--muted-foreground)" }}
          >
            {t}
            {tab === t && (
              <span className="absolute bottom-0 left-0 right-0 h-0.5" style={{ background: "var(--primary)" }} />
            )}
          </button>
        ))}
      </div>

      <div className="max-w-3xl space-y-5">
        {tab === "General" && (
          <>
            <Card title="Workspace identity">
              <Field label="Workspace name">
                <input className="tp-input" defaultValue="Northwind Agency" />
              </Field>
              <Field label="Workspace slug">
                <div className="flex items-center gap-2 text-sm">
                  <span style={{ color: "var(--subtle)" }}>trustpanel.com/c/</span>
                  <input className="tp-input" defaultValue="northwind" />
                </div>
              </Field>
              <Field label="Timezone">
                <select className="tp-input">
                  <option>America/Los_Angeles</option>
                  <option>Europe/London</option>
                  <option>UTC</option>
                </select>
              </Field>
              <Field label="Default language">
                <select className="tp-input">
                  <option>English</option>
                  <option>Spanish</option>
                  <option>French</option>
                </select>
              </Field>
            </Card>
            <Card title="Danger zone" tone="danger">
              <p className="text-sm mb-3" style={{ color: "var(--muted-foreground)" }}>
                Deleting your workspace removes all testimonials, widgets, and forms permanently.
              </p>
              <button className="tp-btn tp-btn-danger">Delete workspace</button>
            </Card>
          </>
        )}

        {tab === "Branding" && (
          <>
            <Card title="Logo & colors">
              <Field label="Logo">
                <div className="border-2 border-dashed rounded-lg p-6 text-center" style={{ borderColor: "var(--border-hover)", color: "var(--subtle)" }}>
                  <Upload size={18} className="mx-auto mb-2" />
                  <div className="text-sm">Drag and drop, or click to upload</div>
                </div>
              </Field>
              <div className="grid grid-cols-2 gap-3">
                <Field label="Primary color">
                  <div className="flex gap-2">
                    <input type="color" defaultValue="#7c6af7" className="w-10 h-9 border-0 rounded cursor-pointer" />
                    <input className="tp-input font-mono" defaultValue="#7c6af7" />
                  </div>
                </Field>
                <Field label="Secondary color">
                  <div className="flex gap-2">
                    <input type="color" defaultValue="#a594f9" className="w-10 h-9 border-0 rounded cursor-pointer" />
                    <input className="tp-input font-mono" defaultValue="#a594f9" />
                  </div>
                </Field>
              </div>
            </Card>
            <Card title="Typography">
              <Field label="Font">
                <div className="grid grid-cols-3 gap-2">
                  {[
                    { n: "Space Grotesk", f: "'Space Grotesk', sans-serif", selected: true },
                    { n: "Inter", f: "Inter, sans-serif" },
                    { n: "Manrope", f: "Manrope, sans-serif" },
                    { n: "Plus Jakarta", f: "'Plus Jakarta Sans', sans-serif" },
                    { n: "DM Sans", f: "'DM Sans', sans-serif" },
                    { n: "Geist", f: "Geist, sans-serif" },
                  ].map((f) => (
                    <button
                      key={f.n}
                      className="tp-card p-3 text-left"
                      style={{
                        background: f.selected ? "var(--primary-soft)" : "var(--surface)",
                        borderColor: f.selected ? "var(--primary)" : "var(--border)",
                      }}
                    >
                      <div className="font-medium" style={{ fontFamily: f.f }}>
                        {f.n}
                      </div>
                    </button>
                  ))}
                </div>
              </Field>
            </Card>
            <Card title="Email sender">
              <Field label="Sender name">
                <input className="tp-input" defaultValue="Northwind Agency" />
              </Field>
              <Field label="Sender address">
                <input className="tp-input" defaultValue="hello@northwind.agency" />
              </Field>
            </Card>
          </>
        )}

        {tab === "Domain" && (
          <Card title="Custom domain">
            <Field label="Domain">
              <input className="tp-input font-mono" placeholder="testimonials.yoursite.com" defaultValue="reviews.northwind.agency" />
            </Field>
            <div className="tp-card p-3 flex items-center gap-2" style={{ background: "rgba(52,211,153,0.08)", borderColor: "var(--success)" }}>
              <Check size={14} style={{ color: "var(--success)" }} />
              <span className="text-sm" style={{ color: "var(--success)" }}>
                Verified · SSL active
              </span>
            </div>
            <Field label="DNS records">
              <div className="tp-card overflow-hidden">
                <table className="w-full text-xs font-mono">
                  <thead style={{ background: "var(--surface)" }}>
                    <tr>
                      {["Type", "Host", "Value"].map((h) => (
                        <th key={h} className="px-3 py-2 text-left">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    <tr style={{ borderTop: "1px solid var(--border)" }}>
                      <td className="px-3 py-2">CNAME</td>
                      <td className="px-3 py-2">reviews</td>
                      <td className="px-3 py-2" style={{ color: "var(--primary-light)" }}>
                        cname.trustpanel.com
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </Field>
            <button className="tp-btn tp-btn-ghost">Re-verify domain</button>
          </Card>
        )}

        {tab === "Notifications" && (
          <Card title="Email notifications">
            {[
              "Email me on new testimonial submission",
              "Email me on auto-approved testimonial",
              "Weekly digest email",
            ].map((l, i) => (
              <Toggle key={l} label={l} on={i !== 1} />
            ))}
            <Field label="Digest day">
              <select className="tp-input">
                <option>Monday</option>
                <option>Friday</option>
                <option>Sunday</option>
              </select>
            </Field>
          </Card>
        )}
      </div>
    </DashboardLayout>
  );
}

function Card({ title, children, tone }: { title: string; children: React.ReactNode; tone?: "danger" }) {
  return (
    <div
      className="tp-card p-6"
      style={tone === "danger" ? { borderColor: "rgba(248,113,113,0.3)" } : undefined}
    >
      <div className="font-semibold mb-4" style={tone === "danger" ? { color: "var(--danger)" } : undefined}>
        {title}
      </div>
      <div className="space-y-4">{children}</div>
    </div>
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

function Toggle({ label, on: initial }: { label: string; on?: boolean }) {
  const [on, setOn] = useState(!!initial);
  return (
    <div className="flex justify-between items-center py-2.5">
      <span className="text-sm">{label}</span>
      <button onClick={() => setOn(!on)} className="w-9 h-5 rounded-full relative" style={{ background: on ? "var(--primary)" : "var(--border)" }}>
        <span className="absolute top-0.5 w-4 h-4 rounded-full bg-white transition-all" style={{ left: on ? 18 : 2 }} />
      </button>
    </div>
  );
}
