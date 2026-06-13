import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Stars } from "@/components/Stars";
import { widgetsApi, type WidgetType, type WidgetSettings } from "@/lib/api/widgets";
import { testimonialsApi } from "@/lib/api/testimonials";
import { useMe, useRequireAuth } from "@/lib/auth";
import { LayoutGrid, Rows3, Award, Bell, GalleryHorizontal, Sparkles, Copy } from "lucide-react";

export const Route = createFileRoute("/dashboard/widgets/create")({
  head: () => ({ meta: [{ title: "Widget builder — TrustPanel" }] }),
  component: WidgetBuilder,
});

const widgetTypes: { key: WidgetType; label: string; icon: React.ElementType }[] = [
  { key: "Carousel", label: "Carousel", icon: GalleryHorizontal },
  { key: "MasonryGrid", label: "Grid", icon: LayoutGrid },
  { key: "Badge", label: "Badge", icon: Award },
  { key: "Popup", label: "Popup", icon: Bell },
  { key: "Slider", label: "Slider", icon: Rows3 },
  { key: "SingleCard", label: "Single Card", icon: Sparkles },
];

const defaultSettings: WidgetSettings = {
  cardStyle: "rounded",
  primaryColor: "#7c6af7",
  backgroundColor: "#ffffff",
  textColor: "#1a1a2e",
  fontSize: "medium",
  animation: "fade",
  darkMode: false,
  showRating: true,
  showAvatar: true,
  showDate: true,
  showSource: false,
};

function WidgetBuilder() {
  useRequireAuth();
  const { data: me } = useMe();
  const navigate = useNavigate();

  const [name, setName] = useState("New Widget");
  const [type, setType] = useState<WidgetType>("MasonryGrid");
  const [settings, setSettings] = useState<WidgetSettings>(defaultSettings);
  const [minRating, setMinRating] = useState<number | undefined>();
  const [featuredOnly, setFeaturedOnly] = useState(false);
  const [customCss, setCustomCss] = useState("");
  const [tab, setTab] = useState<"js" | "iframe">("js");
  const [savedId, setSavedId] = useState<string | null>(null);

  const set = <K extends keyof WidgetSettings>(k: K, v: WidgetSettings[K]) =>
    setSettings((s) => ({ ...s, [k]: v }));

  const { data: testimonialsData } = useQuery({
    queryKey: ["testimonials", me?.workspaceId, "Approved", 1],
    queryFn: () => testimonialsApi.list(me!.workspaceId!, { status: "Approved", pageSize: 3 }),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const preview = testimonialsData?.items ?? [];

  const save = useMutation({
    mutationFn: () =>
      widgetsApi.create({
        workspaceId: me!.workspaceId!,
        type,
        name,
        minimumRating: minRating,
        featuredOnly,
        settings,
        customCss: customCss || undefined,
      }),
    onSuccess: (widget) => {
      setSavedId(widget.id);
    },
  });

  const snippet = (id: string) =>
    tab === "js"
      ? `<div id="tp-widget"></div>\n<script src="https://cdn.trustpanel.com/embed.js"\n  data-widget="${id}"\n  data-target="#tp-widget"\n></script>`
      : `<iframe src="https://embed.trustpanel.com/w/${id}"\n  width="100%" height="420" frameborder="0"></iframe>`;

  return (
    <DashboardLayout
      title="Widget builder"
      action={
        <>
          <button className="tp-btn tp-btn-ghost" onClick={() => navigate({ to: "/dashboard" })}>Cancel</button>
          <button className="tp-btn tp-btn-primary" onClick={() => save.mutate()} disabled={save.isPending || !me?.workspaceId}>
            {save.isPending ? "Saving…" : savedId ? "Saved ✓" : "Publish widget"}
          </button>
        </>
      }
    >
      <div className="grid lg:grid-cols-[420px_1fr] gap-6">
        {/* Config */}
        <div className="space-y-4">
          <Section title="Name">
            <input className="tp-input" value={name} onChange={(e) => setName(e.target.value)} />
          </Section>

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
            <Field label="Primary color">
              <div className="flex gap-2">
                <input type="color" value={settings.primaryColor} onChange={(e) => set("primaryColor", e.target.value)} className="w-10 h-9 rounded cursor-pointer border-0" />
                <input className="tp-input font-mono" value={settings.primaryColor} onChange={(e) => set("primaryColor", e.target.value)} />
              </div>
            </Field>
            <Field label="Card style">
              <div className="grid grid-cols-2 gap-2">
                {(["rounded", "sharp"] as const).map((s) => (
                  <button key={s} className={`tp-btn tp-btn-ghost ${settings.cardStyle === s ? "ring-1 ring-[var(--primary)]" : ""}`} onClick={() => set("cardStyle", s)}>
                    {s.charAt(0).toUpperCase() + s.slice(1)}
                  </button>
                ))}
              </div>
            </Field>
            <div className="space-y-1.5">
              {(["showAvatar", "showRating", "showDate", "showSource"] as const).map((k) => (
                <SwitchRow key={k} label={k.replace("show", "Show ")} on={settings[k]} onChange={(v) => set(k, v)} />
              ))}
            </div>
          </Section>

          <Section title="Filter">
            <Field label="Minimum rating">
              <Stars value={minRating ?? 0} size={20} />
              <div className="flex gap-1 mt-1">
                {[0, 1, 2, 3, 4, 5].map((r) => (
                  <button key={r} className="text-xs tp-btn tp-btn-ghost" style={{ padding: "2px 6px" }} onClick={() => setMinRating(r || undefined)}>
                    {r === 0 ? "Any" : `${r}+`}
                  </button>
                ))}
              </div>
            </Field>
            <SwitchRow label="Featured only" on={featuredOnly} onChange={setFeaturedOnly} />
          </Section>

          <Section title="Advanced">
            <Field label="Custom CSS">
              <textarea
                className="tp-input font-mono text-xs"
                rows={4}
                placeholder=".tp-card { border-radius: 20px; }"
                value={customCss}
                onChange={(e) => setCustomCss(e.target.value)}
              />
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
                <input type="checkbox" checked={settings.darkMode} onChange={(e) => set("darkMode", e.target.checked)} />
                Preview on dark
              </label>
            </div>
            <div
              className="rounded-xl p-8"
              style={{
                background: settings.darkMode ? "#0f0f14" : settings.backgroundColor,
                minHeight: 420,
                color: settings.darkMode ? "#e8e8f0" : settings.textColor,
              }}
            >
              {preview.length === 0 ? (
                <div className="text-center text-sm" style={{ opacity: 0.5 }}>No approved testimonials to preview.</div>
              ) : (
                <div className={type === "MasonryGrid" ? "grid md:grid-cols-3 gap-4" : "flex gap-4 overflow-hidden"}>
                  {preview.map((t) => (
                    <div
                      key={t.id}
                      className="p-5"
                      style={{
                        borderRadius: settings.cardStyle === "rounded" ? 12 : 4,
                        background: settings.darkMode ? "#1e1e2a" : "#fafafa",
                        border: `1px solid ${settings.darkMode ? "rgba(255,255,255,0.07)" : "rgba(0,0,0,0.06)"}`,
                        minWidth: 220,
                      }}
                    >
                      {settings.showRating && t.rating && <Stars value={t.rating} />}
                      <p className="text-sm mt-3 leading-relaxed">"{t.content}"</p>
                      {settings.showAvatar && (
                        <div className="flex items-center gap-2.5 mt-4">
                          <div
                            className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-medium"
                            style={{ background: settings.primaryColor + "33", color: settings.primaryColor }}
                          >
                            {t.submitter.name.slice(0, 2).toUpperCase()}
                          </div>
                          <div>
                            <div className="text-sm font-medium">{t.submitter.name}</div>
                            {t.submitter.company && <div className="text-xs" style={{ opacity: 0.6 }}>{t.submitter.company}</div>}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {savedId && (
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
                  <button
                    className="absolute top-3 right-3 tp-btn tp-btn-ghost"
                    style={{ padding: "4px 8px" }}
                    onClick={() => navigator.clipboard?.writeText(snippet(savedId))}
                  >
                    <Copy size={12} /> Copy
                  </button>
                  <pre style={{ color: "var(--primary-light)" }}>{snippet(savedId)}</pre>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </DashboardLayout>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="tp-card p-5">
      <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--primary-light)" }}>{title}</div>
      <div className="space-y-3">{children}</div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>{label}</label>
      {children}
    </div>
  );
}

function SwitchRow({ label, on, onChange }: { label: string; on: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between text-sm py-1.5">
      <span>{label}</span>
      <button
        onClick={() => onChange(!on)}
        className="w-9 h-5 rounded-full relative"
        style={{ background: on ? "var(--primary)" : "var(--border)" }}
      >
        <span className="absolute top-0.5 w-4 h-4 rounded-full bg-white transition-all" style={{ left: on ? 18 : 2 }} />
      </button>
    </div>
  );
}
