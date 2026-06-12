import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars } from "@/components/Stars";
import { testimonials } from "@/lib/mock-data";
import { LayoutGrid, Rows3, Award, Bell, GalleryHorizontal, Sparkles, Copy } from "lucide-react";

export const Route = createFileRoute("/dashboard/widgets/create")({
  head: () => ({ meta: [{ title: "Widget builder — TrustPanel" }] }),
  component: WidgetBuilder,
});

const widgetTypes = [
  { key: "carousel", label: "Carousel", icon: GalleryHorizontal },
  { key: "grid", label: "Grid", icon: LayoutGrid },
  { key: "badge", label: "Badge", icon: Award },
  { key: "popup", label: "Popup", icon: Bell },
  { key: "slider", label: "Slider", icon: Rows3 },
  { key: "featured", label: "Featured Card", icon: Sparkles },
] as const;

function WidgetBuilder() {
  const [type, setType] = useState<(typeof widgetTypes)[number]["key"]>("grid");
  const [dark, setDark] = useState(false);
  const [tab, setTab] = useState<"js" | "iframe">("js");
  const approved = testimonials.filter((t) => t.status !== "rejected").slice(0, 3);

  return (
    <DashboardLayout
      title="Widget builder"
      action={
        <>
          <button className="tp-btn tp-btn-ghost">Cancel</button>
          <button className="tp-btn tp-btn-primary">Publish changes</button>
        </>
      }
    >
      <div className="grid lg:grid-cols-[420px_1fr] gap-6">
        {/* Config */}
        <div className="space-y-4">
          <Section title="Widget type">
            <div className="grid grid-cols-3 gap-2">
              {widgetTypes.map((w) => (
                <button
                  key={w.key}
                  onClick={() => setType(w.key)}
                  className="p-3 rounded-lg flex flex-col items-center gap-1.5 text-xs"
                  style={{
                    background: type === w.key ? "var(--primary-soft)" : "var(--surface)",
                    border: `1px solid ${type === w.key ? "var(--primary)" : "var(--border)"}`,
                    color: type === w.key ? "var(--primary-light)" : "var(--muted-foreground)",
                  }}
                >
                  <w.icon size={18} />
                  {w.label}
                </button>
              ))}
            </div>
          </Section>

          <Section title="Appearance">
            <Field label="Card style">
              <div className="grid grid-cols-2 gap-2">
                <button className="tp-btn tp-btn-ghost">Rounded</button>
                <button className="tp-btn tp-btn-ghost">Sharp</button>
              </div>
            </Field>
            <Field label="Primary color">
              <div className="flex gap-2">
                <input type="color" defaultValue="#7c6af7" className="w-10 h-9 rounded cursor-pointer border-0" />
                <input className="tp-input font-mono" defaultValue="#7c6af7" />
              </div>
            </Field>
            <Field label="Background">
              <div className="grid grid-cols-3 gap-2 text-xs">
                {["Light", "Dark", "Transparent"].map((b) => (
                  <button key={b} className="tp-btn tp-btn-ghost">
                    {b}
                  </button>
                ))}
              </div>
            </Field>
            <Field label="Font size">
              <input type="range" min={12} max={20} defaultValue={14} className="w-full accent-[var(--primary)]" />
            </Field>
            <div className="space-y-1.5">
              {["Show avatar", "Show company", "Show rating", "Show date", "Show source", "Show verified badge"].map((l, i) => (
                <SwitchRow key={l} label={l} on={i < 4} />
              ))}
            </div>
          </Section>

          <Section title="Filter">
            <Field label="Minimum rating">
              <Stars value={4} size={20} />
            </Field>
            <Field label="Show">
              <select className="tp-input">
                <option>All approved</option>
                <option>Featured only</option>
                <option>Specific tags</option>
              </select>
            </Field>
            <Field label="Max testimonials">
              <input className="tp-input" type="number" defaultValue={12} />
            </Field>
          </Section>

          <Section title="Advanced">
            <Field label="Custom CSS">
              <textarea className="tp-input font-mono text-xs" rows={4} placeholder=".tp-card { border-radius: 20px; }" />
            </Field>
            <Field label="Animation">
              <select className="tp-input">
                <option>Fade</option>
                <option>Slide</option>
                <option>None</option>
              </select>
            </Field>
          </Section>
        </div>

        {/* Preview */}
        <div className="space-y-4">
          <div className="tp-card p-4">
            <div className="flex justify-between items-center mb-4">
              <div className="text-xs uppercase tracking-wider" style={{ color: "var(--subtle)" }}>
                Live preview · {type}
              </div>
              <label className="text-xs flex items-center gap-2" style={{ color: "var(--muted-foreground)" }}>
                <input type="checkbox" checked={dark} onChange={(e) => setDark(e.target.checked)} />
                Preview on dark
              </label>
            </div>
            <div
              className="rounded-xl p-8"
              style={{ background: dark ? "#0f0f14" : "#ffffff", minHeight: 420, color: dark ? "#e8e8f0" : "#0f0f14" }}
            >
              <div
                className={
                  type === "grid"
                    ? "grid md:grid-cols-3 gap-4"
                    : type === "carousel" || type === "slider"
                      ? "flex gap-4 overflow-hidden"
                      : "grid md:grid-cols-2 gap-4"
                }
              >
                {approved.map((t) => (
                  <div
                    key={t.id}
                    className="rounded-xl p-5"
                    style={{
                      background: dark ? "#1e1e2a" : "#fafafa",
                      border: `1px solid ${dark ? "rgba(255,255,255,0.07)" : "rgba(0,0,0,0.06)"}`,
                      minWidth: 220,
                    }}
                  >
                    <Stars value={t.rating} />
                    <p className="text-sm mt-3 leading-relaxed">"{t.text}"</p>
                    <div className="flex items-center gap-2.5 mt-4">
                      <Avatar name={t.name} color={t.avatarColor} size={32} />
                      <div>
                        <div className="text-sm font-medium">{t.name}</div>
                        <div className="text-xs" style={{ opacity: 0.6 }}>
                          {t.company}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="tp-card">
            <div className="flex gap-1 p-4 pb-0">
              {(["js", "iframe"] as const).map((k) => (
                <button
                  key={k}
                  onClick={() => setTab(k)}
                  className="px-3 py-1.5 rounded-md text-xs"
                  style={{
                    background: tab === k ? "var(--primary-soft)" : "transparent",
                    color: tab === k ? "var(--primary-light)" : "var(--muted-foreground)",
                  }}
                >
                  {k === "js" ? "JavaScript snippet" : "iFrame"}
                </button>
              ))}
            </div>
            <div className="p-4">
              <div className="relative font-mono text-xs rounded-lg p-4" style={{ background: "var(--surface)" }}>
                <button className="absolute top-3 right-3 tp-btn tp-btn-ghost" style={{ padding: "4px 8px" }}>
                  <Copy size={12} /> Copy
                </button>
                <pre style={{ color: "var(--primary-light)" }}>
{tab === "js"
  ? `<div id="tp-widget"></div>
<script src="https://cdn.trustpanel.com/embed.js"
  data-widget="wgt_a8f3"
  data-target="#tp-widget"
></script>`
  : `<iframe src="https://embed.trustpanel.com/w/wgt_a8f3"
  width="100%" height="420" frameborder="0"></iframe>`}
                </pre>
              </div>
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="tp-card p-5">
      <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--primary-light)" }}>
        {title}
      </div>
      <div className="space-y-3">{children}</div>
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

function SwitchRow({ label, on: initial }: { label: string; on?: boolean }) {
  const [on, setOn] = useState(!!initial);
  return (
    <div className="flex items-center justify-between text-sm py-1.5">
      <span>{label}</span>
      <button
        onClick={() => setOn(!on)}
        className="w-9 h-5 rounded-full relative"
        style={{ background: on ? "var(--primary)" : "var(--border)" }}
      >
        <span className="absolute top-0.5 w-4 h-4 rounded-full bg-white transition-all" style={{ left: on ? 18 : 2 }} />
      </button>
    </div>
  );
}
