import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Upload, Calendar, Send, CheckCircle } from "lucide-react";
import { emailApi } from "@/lib/api/email";
import { formsApi } from "@/lib/api/forms";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/requests")({
  head: () => ({ meta: [{ title: "Send requests — TrustPanel" }] }),
  component: Requests,
});

const statusTone: Record<string, "neutral" | "info" | "warning" | "success"> = {
  Sent: "neutral",
  Opened: "info",
  Clicked: "warning",
  Submitted: "success",
};

function Requests() {
  useRequireAuth();
  const { data: me } = useMe();
  const [tab, setTab] = useState<"single" | "bulk" | "history">("single");

  // Single request form state.
  const [recipientName, setRecipientName] = useState("");
  const [recipientEmail, setRecipientEmail] = useState("");
  const [selectedFormId, setSelectedFormId] = useState<string>("");
  const [customMessage, setCustomMessage] = useState("");
  const [sent, setSent] = useState(false);

  const { data: formsData } = useQuery({
    queryKey: ["forms", me?.workspaceId],
    queryFn: () => formsApi.list(me?.workspaceId),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });
  const forms = formsData?.items ?? [];

  const sendRequest = useMutation({
    mutationFn: () =>
      emailApi.sendRequest({
        workspaceId: me!.workspaceId!,
        recipientName,
        recipientEmail,
        formId: selectedFormId || undefined,
        customMessage: customMessage || undefined,
      }),
    onSuccess: () => {
      setSent(true);
      setRecipientName("");
      setRecipientEmail("");
      setCustomMessage("");
    },
  });

  const canSend = !!recipientName.trim() && !!recipientEmail.trim() && !!me?.workspaceId;

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
            onClick={() => { setTab(k); setSent(false); }}
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
          {sent && (
            <div className="flex items-center gap-2 text-sm tp-card p-3" style={{ color: "var(--success)", borderColor: "var(--success)", background: "rgba(52,211,153,0.06)" }}>
              <CheckCircle size={15} />
              Request sent! The recipient will receive an email shortly.
            </div>
          )}
          {sendRequest.isError && (
            <div className="text-sm tp-card p-3" style={{ color: "var(--danger)", borderColor: "var(--danger)" }}>
              Failed to send request. Please try again.
            </div>
          )}
          <div className="grid md:grid-cols-2 gap-4">
            <Field label="Recipient name">
              <input
                className="tp-input"
                placeholder="Sarah Kowalski"
                value={recipientName}
                onChange={(e) => setRecipientName(e.target.value)}
              />
            </Field>
            <Field label="Email">
              <input
                className="tp-input"
                type="email"
                placeholder="sarah@northwind.studios"
                value={recipientEmail}
                onChange={(e) => setRecipientEmail(e.target.value)}
              />
            </Field>
          </div>
          <Field label="Form">
            <select
              className="tp-input"
              value={selectedFormId}
              onChange={(e) => setSelectedFormId(e.target.value)}
            >
              <option value="">— Any active form —</option>
              {forms.map((f) => (
                <option key={f.id} value={f.id}>{f.name}</option>
              ))}
            </select>
          </Field>
          <Field label="Custom message (optional)">
            <textarea
              className="tp-input"
              rows={4}
              placeholder="Hi Sarah — would love to hear how things have been going with the rebrand. Mind sharing a quick note?"
              value={customMessage}
              onChange={(e) => setCustomMessage(e.target.value)}
            />
          </Field>
          <button
            className="tp-btn tp-btn-primary"
            onClick={() => { setSent(false); sendRequest.mutate(); }}
            disabled={!canSend || sendRequest.isPending}
          >
            <Send size={14} />
            {sendRequest.isPending ? "Sending…" : "Send request"}
          </button>
        </div>
      )}

      {tab === "bulk" && (
        <div className="tp-card p-6 max-w-2xl space-y-4">
          <div className="border-2 border-dashed rounded-lg p-8 text-center" style={{ borderColor: "var(--border-hover)" }}>
            <Upload size={20} className="mx-auto mb-2" style={{ color: "var(--primary-light)" }} />
            <div className="text-sm">Upload CSV with columns: name, email</div>
            <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>or paste emails below</div>
          </div>
          <textarea className="tp-input font-mono text-xs" rows={5} placeholder="sarah@…&#10;marco@…" />
          <div className="grid md:grid-cols-2 gap-4">
            <Field label="Form">
              <select className="tp-input">
                <option value="">— Any active form —</option>
                {forms.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
              </select>
            </Field>
          </div>
          <Field label="Schedule">
            <div className="flex gap-2">
              <button className="tp-btn tp-btn-primary" disabled>Send now</button>
              <button className="tp-btn tp-btn-ghost" disabled>
                <Calendar size={14} /> Schedule
              </button>
            </div>
          </Field>
          <div className="text-sm tp-card p-3" style={{ background: "var(--surface)", color: "var(--muted-foreground)" }}>
            Bulk send is coming soon. Use single request for now.
          </div>
        </div>
      )}

      {tab === "history" && (
        <div className="tp-card p-8 text-center" style={{ color: "var(--subtle)" }}>
          <div className="text-sm">Email send history will appear here once you start sending requests.</div>
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
