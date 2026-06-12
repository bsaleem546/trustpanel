import { createFileRoute, Link } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { forms } from "@/lib/mock-data";
import { Plus, Copy, Edit, Trash2, ExternalLink } from "lucide-react";

export const Route = createFileRoute("/dashboard/forms")({
  head: () => ({ meta: [{ title: "Forms — TrustPanel" }] }),
  component: FormsList,
});

function FormsList() {
  return (
    <DashboardLayout
      title="Collection forms"
      action={
        <Link to="/dashboard/forms/create" className="tp-btn tp-btn-primary">
          <Plus size={14} /> Create form
        </Link>
      }
    >
      <div className="grid md:grid-cols-2 gap-4">
        {forms.map((f) => (
          <div key={f.id} className="tp-card p-6">
            <div className="flex items-start justify-between mb-4">
              <div>
                <div className="font-semibold">{f.name}</div>
                <div className="text-xs mt-1" style={{ color: "var(--subtle)" }}>
                  Created {f.created}
                </div>
              </div>
              <Pill tone={f.active ? "success" : "neutral"}>{f.active ? "Active" : "Paused"}</Pill>
            </div>
            <div className="grid grid-cols-2 gap-3 mb-4">
              <div className="tp-card p-3" style={{ background: "var(--surface)" }}>
                <div className="text-xl font-semibold">{f.submissions}</div>
                <div className="text-xs" style={{ color: "var(--subtle)" }}>
                  submissions
                </div>
              </div>
              <div className="tp-card p-3" style={{ background: "var(--surface)" }}>
                <div className="text-xl font-semibold" style={{ color: "var(--success)" }}>
                  {f.conversion}%
                </div>
                <div className="text-xs" style={{ color: "var(--subtle)" }}>
                  conversion
                </div>
              </div>
            </div>
            <div className="flex gap-2 mb-4">
              {f.types.map((t) => (
                <Pill key={t} tone="primary">
                  {t.toUpperCase()}
                </Pill>
              ))}
            </div>
            <div className="flex gap-2">
              <Link to="/dashboard/forms/create" className="tp-btn tp-btn-ghost flex-1" style={{ fontSize: 12 }}>
                <Edit size={12} /> Edit
              </Link>
              <button className="tp-btn tp-btn-ghost" style={{ fontSize: 12 }}>
                <Copy size={12} /> Copy link
              </button>
              <button className="tp-btn tp-btn-ghost" style={{ fontSize: 12 }}>
                <ExternalLink size={12} />
              </button>
              <button className="tp-btn tp-btn-ghost" style={{ fontSize: 12, color: "var(--danger)" }}>
                <Trash2 size={12} />
              </button>
            </div>
          </div>
        ))}
      </div>
    </DashboardLayout>
  );
}
