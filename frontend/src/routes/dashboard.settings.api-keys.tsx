import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Plus, Copy, AlertTriangle, Trash2, Webhook, Check } from "lucide-react";
import { apiKeysApi, webhooksApi, type ApiKey, type WebhookEndpoint } from "@/lib/api/apikeys";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/settings/api-keys")({
  head: () => ({ meta: [{ title: "API keys — TrustPanel" }] }),
  component: ApiKeys,
});

function ApiKeys() {
  useRequireAuth();
  const { data: me } = useMe();
  const workspaceId = me?.workspaceId;
  const queryClient = useQueryClient();
  const invalidateKeys = () => queryClient.invalidateQueries({ queryKey: ["apikeys"] });
  const invalidateWebhooks = () => queryClient.invalidateQueries({ queryKey: ["webhooks"] });

  // New key creation
  const [showCreate, setShowCreate] = useState(false);
  const [newKeyName, setNewKeyName] = useState("");
  const [plaintextKey, setPlaintextKey] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  // Rename state
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameValue, setRenameValue] = useState("");

  // Webhook form
  const [showWebhookForm, setShowWebhookForm] = useState(false);
  const [webhookUrl, setWebhookUrl] = useState("");

  const { data: keys = [], isLoading } = useQuery({
    queryKey: ["apikeys", workspaceId],
    queryFn: () => apiKeysApi.list(workspaceId!),
    enabled: !!workspaceId,
    staleTime: 60_000,
  });

  // Webhooks — backend doesn't expose a list endpoint yet; we track locally
  const { data: webhooks = [] } = useQuery<WebhookEndpoint[]>({
    queryKey: ["webhooks", workspaceId],
    queryFn: async () => [],
    enabled: !!workspaceId,
    staleTime: Infinity,
  });

  const createKey = useMutation({
    mutationFn: () => apiKeysApi.create(workspaceId!, newKeyName.trim()),
    onSuccess: (res) => {
      setPlaintextKey(res.plaintextKey);
      setNewKeyName("");
      setShowCreate(false);
      invalidateKeys();
    },
  });

  const renameKey = useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) =>
      apiKeysApi.rename(id, workspaceId!, name),
    onSuccess: () => { setRenamingId(null); invalidateKeys(); },
  });

  const revokeKey = useMutation({
    mutationFn: (id: string) => apiKeysApi.revoke(id, workspaceId!),
    onSuccess: invalidateKeys,
  });

  const createWebhook = useMutation({
    mutationFn: () => webhooksApi.create(workspaceId!, webhookUrl.trim()),
    onSuccess: (endpoint) => {
      queryClient.setQueryData<WebhookEndpoint[]>(["webhooks", workspaceId], (old = []) => [...old, endpoint]);
      setWebhookUrl("");
      setShowWebhookForm(false);
    },
  });

  const removeWebhook = useMutation({
    mutationFn: (id: string) => webhooksApi.remove(id, workspaceId!),
    onSuccess: (_, id) => {
      queryClient.setQueryData<WebhookEndpoint[]>(["webhooks", workspaceId], (old = []) => old.filter((w) => w.id !== id));
      invalidateWebhooks();
    },
  });

  function copyKey() {
    if (!plaintextKey) return;
    navigator.clipboard.writeText(plaintextKey).then(() => { setCopied(true); setTimeout(() => setCopied(false), 2000); });
  }

  return (
    <DashboardLayout
      title="API keys"
      action={
        <button className="tp-btn tp-btn-primary" onClick={() => setShowCreate(true)}>
          <Plus size={14} /> Create key
        </button>
      }
    >
      {/* Plaintext key reveal */}
      {plaintextKey && (
        <div className="tp-card p-5 mb-6" style={{ background: "rgba(251,191,36,0.06)", borderColor: "var(--warning)" }}>
          <div className="flex items-center gap-2 mb-3">
            <AlertTriangle size={14} style={{ color: "var(--warning)" }} />
            <span className="font-semibold" style={{ color: "var(--warning)" }}>
              This is the only time your key will be shown
            </span>
          </div>
          <div className="font-mono text-sm tp-card p-3 flex items-center justify-between" style={{ background: "var(--surface)" }}>
            <span style={{ color: "var(--primary-light)" }}>{plaintextKey}</span>
            <button className="tp-btn tp-btn-ghost flex items-center gap-1" style={{ padding: "4px 8px" }} onClick={copyKey}>
              {copied ? <Check size={12} /> : <Copy size={12} />} {copied ? "Copied" : "Copy"}
            </button>
          </div>
          <button onClick={() => setPlaintextKey(null)} className="tp-btn tp-btn-primary mt-4">
            I've saved it
          </button>
        </div>
      )}

      {/* Create key form */}
      {showCreate && (
        <div className="tp-card p-5 mb-6 max-w-md">
          <div className="font-semibold mb-3">New API key</div>
          <input
            className="tp-input mb-3"
            placeholder="Key name (e.g. Production server)"
            value={newKeyName}
            onChange={(e) => setNewKeyName(e.target.value)}
            onKeyDown={(e) => { if (e.key === "Enter" && newKeyName.trim()) createKey.mutate(); }}
          />
          {createKey.isError && (
            <p className="text-xs mb-3" style={{ color: "var(--danger)" }}>
              {(createKey.error as Error).message ?? "Failed to create key."}
            </p>
          )}
          <div className="flex gap-2">
            <button className="tp-btn tp-btn-primary" onClick={() => createKey.mutate()} disabled={createKey.isPending || !newKeyName.trim() || !workspaceId}>
              {createKey.isPending ? "Creating…" : "Create"}
            </button>
            <button className="tp-btn tp-btn-ghost" onClick={() => { setShowCreate(false); setNewKeyName(""); }}>Cancel</button>
          </div>
        </div>
      )}

      {/* Keys table */}
      <div className="tp-card overflow-hidden mb-8">
        <table className="w-full text-sm">
          <thead style={{ background: "var(--surface)" }}>
            <tr>
              {["Name", "Key", "Last used", "Created", ""].map((h) => (
                <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={5} className="px-5 py-4 text-sm" style={{ color: "var(--subtle)" }}>Loading…</td></tr>
            )}
            {!isLoading && keys.length === 0 && (
              <tr><td colSpan={5} className="px-5 py-6 text-sm text-center" style={{ color: "var(--subtle)" }}>No API keys yet.</td></tr>
            )}
            {keys.map((k: ApiKey) => (
              <tr key={k.id} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3 font-medium">
                  {renamingId === k.id ? (
                    <div className="flex gap-2 items-center">
                      <input
                        className="tp-input text-sm py-1"
                        value={renameValue}
                        onChange={(e) => setRenameValue(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter") renameKey.mutate({ id: k.id, name: renameValue });
                          if (e.key === "Escape") setRenamingId(null);
                        }}
                        autoFocus
                      />
                      <button className="tp-btn tp-btn-ghost" style={{ padding: "2px 8px", fontSize: 12 }}
                        onClick={() => renameKey.mutate({ id: k.id, name: renameValue })}
                        disabled={renameKey.isPending}>
                        {renameKey.isPending ? "…" : "Save"}
                      </button>
                    </div>
                  ) : (
                    <button className="hover:underline text-left" onClick={() => { setRenamingId(k.id); setRenameValue(k.name); }}>
                      {k.name}
                    </button>
                  )}
                </td>
                <td className="px-5 py-3 font-mono text-xs" style={{ color: "var(--muted-foreground)" }}>
                  {k.keyPreview}••••••••
                </td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                  {k.lastUsedAt ? new Date(k.lastUsedAt).toLocaleDateString() : "Never"}
                </td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                  {new Date(k.createdAt).toLocaleDateString()}
                </td>
                <td className="px-5 py-3 text-right">
                  <button
                    className="text-xs flex items-center gap-1 ml-auto"
                    style={{ color: "var(--danger)" }}
                    onClick={() => { if (confirm(`Revoke "${k.name}"?`)) revokeKey.mutate(k.id); }}
                    disabled={revokeKey.isPending}
                  >
                    <Trash2 size={11} /> Revoke
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Webhooks section */}
      <div className="mb-8">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Webhook size={16} style={{ color: "var(--primary-light)" }} />
            <h2 className="font-semibold">Webhook endpoints</h2>
          </div>
          <button className="tp-btn tp-btn-ghost" onClick={() => setShowWebhookForm(true)}>
            <Plus size={14} /> Add endpoint
          </button>
        </div>

        {showWebhookForm && (
          <div className="tp-card p-5 mb-4 max-w-lg">
            <div className="font-semibold mb-3">New webhook endpoint</div>
            <input
              className="tp-input mb-3 font-mono"
              placeholder="https://yoursite.com/hooks/trustpanel"
              value={webhookUrl}
              onChange={(e) => setWebhookUrl(e.target.value)}
            />
            <p className="text-xs mb-3" style={{ color: "var(--muted-foreground)" }}>
              We'll send a POST request to this URL whenever a testimonial is created.
            </p>
            {createWebhook.isError && (
              <p className="text-xs mb-3" style={{ color: "var(--danger)" }}>
                {(createWebhook.error as Error).message ?? "Failed to register endpoint."}
              </p>
            )}
            <div className="flex gap-2">
              <button className="tp-btn tp-btn-primary" onClick={() => createWebhook.mutate()} disabled={createWebhook.isPending || !webhookUrl.trim()}>
                {createWebhook.isPending ? "Registering…" : "Register"}
              </button>
              <button className="tp-btn tp-btn-ghost" onClick={() => { setShowWebhookForm(false); setWebhookUrl(""); }}>Cancel</button>
            </div>
          </div>
        )}

        <div className="tp-card overflow-hidden">
          {webhooks.length === 0 ? (
            <div className="px-5 py-6 text-sm text-center" style={{ color: "var(--subtle)" }}>
              No webhook endpoints registered.
            </div>
          ) : (
            <table className="w-full text-sm">
              <tbody>
                {webhooks.map((w: WebhookEndpoint) => (
                  <tr key={w.id} style={{ borderTop: "1px solid var(--border)" }}>
                    <td className="px-5 py-3 font-mono text-xs" style={{ color: "var(--primary-light)" }}>{w.url}</td>
                    <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                      {new Date(w.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-5 py-3 text-right">
                      <button
                        className="text-xs flex items-center gap-1 ml-auto"
                        style={{ color: "var(--danger)" }}
                        onClick={() => { if (confirm("Remove this webhook?")) removeWebhook.mutate(w.id); }}
                        disabled={removeWebhook.isPending}
                      >
                        <Trash2 size={11} /> Remove
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Quick API reference */}
      <div className="tp-card p-6">
        <div className="font-semibold mb-4">Quick API reference</div>
        <div className="space-y-3 font-mono text-xs">
          <CodeBlock label="Base URL" code="https://your-instance.com/api/v1" />
          <CodeBlock label="List testimonials" code={`curl /api/v1/testimonials?workspaceId=<id> \\
  -H "Authorization: ApiKey tp_live_..."`} />
          <CodeBlock label="Create testimonial" code={`curl -X POST /api/v1/testimonials \\
  -H "Authorization: ApiKey tp_live_..." \\
  -d '{ "content": "...", "submitterName": "Sarah K.", "rating": 5 }'`} />
        </div>
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
