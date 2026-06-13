import { createFileRoute, Link } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { formsApi, type Form } from "@/lib/api/forms";
import { useMe, useRequireAuth } from "@/lib/auth";
import { Plus, Copy, Edit, Trash2, ExternalLink } from "lucide-react";

export const Route = createFileRoute("/dashboard/forms")({
  head: () => ({ meta: [{ title: "Forms — TrustPanel" }] }),
  component: FormsList,
});

function FormsList() {
  useRequireAuth();
  const { data: me } = useMe();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ["forms", me?.workspaceId],
    queryFn: () => formsApi.list(me?.workspaceId),
    enabled: !!me,
    staleTime: 30_000,
  });

  const remove = useMutation({
    mutationFn: (id: string) => formsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["forms"] }),
  });

  const forms = data?.items ?? [];

  return (
    <DashboardLayout
      title="Collection forms"
      action={
        <Link to="/dashboard/forms/create" className="tp-btn tp-btn-primary">
          <Plus size={14} /> Create form
        </Link>
      }
    >
      {isLoading && (
        <div className="text-sm" style={{ color: "var(--subtle)" }}>Loading forms…</div>
      )}
      <div className="grid md:grid-cols-2 gap-4">
        {forms.map((f: Form) => (
          <div key={f.id} className="tp-card p-6">
            <div className="flex items-start justify-between mb-4">
              <div>
                <div className="font-semibold">{f.name}</div>
                <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>
                  Created {new Date(f.createdAt).toLocaleDateString()}
                </div>
              </div>
              <Pill tone={f.isActive ? "success" : "neutral"}>{f.isActive ? "Active" : "Paused"}</Pill>
            </div>
            <div className="flex gap-2 mb-4">
              <Pill tone="primary">{f.allowedSubmissionType.toUpperCase()}</Pill>
            </div>
            <div className="flex gap-2">
              <Link to="/dashboard/forms/create" className="tp-btn tp-btn-ghost flex-1" style={{ fontSize: 12 }}>
                <Edit size={12} /> Edit
              </Link>
              <button
                className="tp-btn tp-btn-ghost"
                style={{ fontSize: 12 }}
                onClick={() => navigator.clipboard?.writeText(`${window.location.origin}/c/${f.workspaceId}/${f.slug}`)}
              >
                <Copy size={12} /> Copy link
              </button>
              <a
                href={`/c/${f.workspaceId}/${f.slug}`}
                target="_blank"
                rel="noreferrer"
                className="tp-btn tp-btn-ghost"
                style={{ fontSize: 12 }}
              >
                <ExternalLink size={12} />
              </a>
              <button
                className="tp-btn tp-btn-ghost"
                style={{ fontSize: 12, color: "var(--danger)" }}
                onClick={() => { if (confirm(`Delete "${f.name}"?`)) remove.mutate(f.id); }}
              >
                <Trash2 size={12} />
              </button>
            </div>
          </div>
        ))}
        {!isLoading && forms.length === 0 && (
          <div className="tp-card p-10 text-center col-span-2" style={{ color: "var(--subtle)" }}>
            No forms yet. Create your first one.
          </div>
        )}
      </div>
    </DashboardLayout>
  );
}
