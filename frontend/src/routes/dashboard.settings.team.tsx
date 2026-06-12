import { createFileRoute } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Pill } from "@/components/Stars";
import { Plus, Trash2 } from "lucide-react";

export const Route = createFileRoute("/dashboard/settings/team")({
  head: () => ({ meta: [{ title: "Team — TrustPanel" }] }),
  component: TeamSettings,
});

const members = [
  { name: "Alex Mendez", email: "alex@northwind.agency", role: "Owner", joined: "Jan 2025", active: "now", color: "#7c6af7" },
  { name: "Jamie Liu", email: "jamie@northwind.agency", role: "Admin", joined: "Feb 2025", active: "2h ago", color: "#34d399" },
  { name: "Theo Carter", email: "theo@northwind.agency", role: "Viewer", joined: "Apr 2025", active: "1 day ago", color: "#60a5fa" },
];

const invites = [
  { email: "noor@northwind.agency", role: "Admin", sent: "2 days ago", expires: "5 days" },
  { email: "june@northwind.agency", role: "Viewer", sent: "5 days ago", expires: "2 days" },
];

function TeamSettings() {
  return (
    <DashboardLayout
      title="Team"
      action={
        <button className="tp-btn tp-btn-primary">
          <Plus size={14} /> Invite member
        </button>
      }
    >
      <div className="tp-card overflow-hidden mb-6">
        <table className="w-full text-sm">
          <thead style={{ background: "var(--surface)" }}>
            <tr>
              {["Member", "Role", "Joined", "Last active", ""].map((h) => (
                <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {members.map((m) => (
              <tr key={m.email} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3 flex items-center gap-3">
                  <Avatar name={m.name} color={m.color} size={32} />
                  <div>
                    <div className="font-medium">{m.name}</div>
                    <div className="text-xs" style={{ color: "var(--subtle)" }}>
                      {m.email}
                    </div>
                  </div>
                </td>
                <td className="px-5 py-3">
                  <Pill tone={m.role === "Owner" ? "primary" : m.role === "Admin" ? "info" : "neutral"}>{m.role}</Pill>
                </td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>{m.joined}</td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>{m.active}</td>
                <td className="px-5 py-3 text-right">
                  {m.role !== "Owner" && (
                    <button className="text-xs" style={{ color: "var(--danger)" }}>
                      <Trash2 size={12} className="inline" /> Remove
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="tp-card overflow-hidden">
        <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
          <div className="font-semibold">Pending invitations</div>
        </div>
        <table className="w-full text-sm">
          <tbody>
            {invites.map((inv) => (
              <tr key={inv.email} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3">{inv.email}</td>
                <td className="px-5 py-3"><Pill tone="info">{inv.role}</Pill></td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>Sent {inv.sent}</td>
                <td className="px-5 py-3" style={{ color: "var(--subtle)" }}>Expires in {inv.expires}</td>
                <td className="px-5 py-3 text-right">
                  <button className="text-xs mr-3" style={{ color: "var(--primary-light)" }}>Resend</button>
                  <button className="text-xs" style={{ color: "var(--subtle)" }}>Cancel</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </DashboardLayout>
  );
}
