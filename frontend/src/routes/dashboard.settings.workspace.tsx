import { createFileRoute } from "@tanstack/react-router";
import { useState, useEffect } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Check, Upload } from "lucide-react";
import { workspacesApi } from "@/lib/api/workspaces";
import { useCurrentWorkspace, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/settings/workspace")({
  head: () => ({ meta: [{ title: "Workspace settings — TrustPanel" }] }),
  component: WorkspaceSettings,
});

const tabs = ["General", "Branding", "Domain", "Notifications"] as const;

function WorkspaceSettings() {
  useRequireAuth();
  const { data: workspace } = useCurrentWorkspace();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<(typeof tabs)[number]>("General");

  // General tab state
  const [wsName, setWsName] = useState("");
  useEffect(() => { if (workspace?.name) setWsName(workspace.name); }, [workspace?.name]);

  // Branding tab state
  const [primaryColor, setPrimaryColor] = useState("#7c6af7");
  const [secondaryColor, setSecondaryColor] = useState("#a594f9");
  const [emailFromName, setEmailFromName] = useState("");
  const [emailFromAddress, setEmailFromAddress] = useState("");
  useEffect(() => {
    if (workspace) {
      if (workspace.branding.primaryColor) setPrimaryColor(workspace.branding.primaryColor);
      if (workspace.branding.secondaryColor) setSecondaryColor(workspace.branding.secondaryColor);
      if (workspace.emailFrom.fromName) setEmailFromName(workspace.emailFrom.fromName);
      if (workspace.emailFrom.fromEmail) setEmailFromAddress(workspace.emailFrom.fromEmail);
    }
  }, [workspace]);

  // Domain tab state
  const [domain, setDomain] = useState("");
  useEffect(() => { if (workspace?.customDomain) setDomain(workspace.customDomain); }, [workspace?.customDomain]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["workspace"] });

  const updateName = useMutation({
    mutationFn: () => workspacesApi.update(workspace!.id, wsName),
    onSuccess: invalidate,
  });

  const updateBranding = useMutation({
    mutationFn: () => workspacesApi.updateBranding(workspace!.id, {
      primaryColor, secondaryColor, emailFromName, emailFromAddress,
    }),
    onSuccess: invalidate,
  });

  const setCustomDomain = useMutation({
    mutationFn: () => workspacesApi.setCustomDomain(workspace!.id, domain),
    onSuccess: invalidate,
  });

  const verifyDomain = useMutation({
    mutationFn: () => workspacesApi.verifyCustomDomain(workspace!.id),
    onSuccess: invalidate,
  });

  const removeWorkspace = useMutation({
    mutationFn: () => workspacesApi.remove(workspace!.id),
  });

  const [saveMsg, setSaveMsg] = useState<string | null>(null);
  async function save(fn: () => Promise<unknown>) {
    setSaveMsg(null);
    try { await fn(); setSaveMsg("Saved."); setTimeout(() => setSaveMsg(null), 2000); }
    catch { setSaveMsg("Failed to save."); }
  }

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
                <input className="tp-input" value={wsName} onChange={(e) => setWsName(e.target.value)} />
              </Field>
              <Field label="Workspace slug">
                <div className="flex items-center gap-2 text-sm">
                  <span style={{ color: "var(--subtle)" }}>trustpanel.com/c/</span>
                  <input className="tp-input" value={workspace?.slug ?? ""} readOnly style={{ color: "var(--subtle)" }} />
                </div>
              </Field>
              {saveMsg && <p className="text-xs" style={{ color: saveMsg === "Saved." ? "var(--success)" : "var(--danger)" }}>{saveMsg}</p>}
              <button className="tp-btn tp-btn-primary" onClick={() => save(() => updateName.mutateAsync())} disabled={updateName.isPending || !workspace}>
                {updateName.isPending ? "Saving…" : "Save changes"}
              </button>
            </Card>
            <Card title="Danger zone" tone="danger">
              <p className="text-sm mb-3" style={{ color: "var(--muted-foreground)" }}>
                Deleting your workspace removes all testimonials, widgets, and forms permanently.
              </p>
              <button
                className="tp-btn tp-btn-danger"
                disabled={removeWorkspace.isPending || !workspace}
                onClick={() => { if (confirm("Delete this workspace permanently?")) removeWorkspace.mutate(); }}
              >
                Delete workspace
              </button>
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
                    <input type="color" value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} className="w-10 h-9 border-0 rounded cursor-pointer" />
                    <input className="tp-input font-mono" value={primaryColor} onChange={(e) => setPrimaryColor(e.target.value)} />
                  </div>
                </Field>
                <Field label="Secondary color">
                  <div className="flex gap-2">
                    <input type="color" value={secondaryColor} onChange={(e) => setSecondaryColor(e.target.value)} className="w-10 h-9 border-0 rounded cursor-pointer" />
                    <input className="tp-input font-mono" value={secondaryColor} onChange={(e) => setSecondaryColor(e.target.value)} />
                  </div>
                </Field>
              </div>
            </Card>
            <Card title="Email sender">
              <Field label="Sender name">
                <input className="tp-input" value={emailFromName} onChange={(e) => setEmailFromName(e.target.value)} />
              </Field>
              <Field label="Sender address">
                <input className="tp-input" value={emailFromAddress} onChange={(e) => setEmailFromAddress(e.target.value)} />
              </Field>
            </Card>
            {saveMsg && <p className="text-xs" style={{ color: saveMsg === "Saved." ? "var(--success)" : "var(--danger)" }}>{saveMsg}</p>}
            <button className="tp-btn tp-btn-primary" onClick={() => save(() => updateBranding.mutateAsync())} disabled={updateBranding.isPending || !workspace}>
              {updateBranding.isPending ? "Saving…" : "Save branding"}
            </button>
          </>
        )}

        {tab === "Domain" && (
          <Card title="Custom domain">
            <Field label="Domain">
              <input className="tp-input font-mono" placeholder="testimonials.yoursite.com" value={domain} onChange={(e) => setDomain(e.target.value)} />
            </Field>
            {workspace?.domainVerifiedAt && (
              <div className="tp-card p-3 flex items-center gap-2" style={{ background: "rgba(52,211,153,0.08)", borderColor: "var(--success)" }}>
                <Check size={14} style={{ color: "var(--success)" }} />
                <span className="text-sm" style={{ color: "var(--success)" }}>
                  Verified · SSL active
                </span>
              </div>
            )}
            {saveMsg && <p className="text-xs" style={{ color: saveMsg === "Saved." ? "var(--success)" : "var(--danger)" }}>{saveMsg}</p>}
            <div className="flex gap-2">
              <button className="tp-btn tp-btn-primary" onClick={() => save(() => setCustomDomain.mutateAsync())} disabled={setCustomDomain.isPending || !workspace || !domain}>
                {setCustomDomain.isPending ? "Saving…" : "Save domain"}
              </button>
              <button className="tp-btn tp-btn-ghost" onClick={() => save(() => verifyDomain.mutateAsync())} disabled={verifyDomain.isPending || !workspace}>
                {verifyDomain.isPending ? "Verifying…" : "Re-verify domain"}
              </button>
            </div>
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
