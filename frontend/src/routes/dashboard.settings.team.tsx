import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Pill } from "@/components/Stars";
import { Plus, Trash2 } from "lucide-react";
import { teamApi, type WorkspaceRole, type TeamMember } from "@/lib/api/team";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/settings/team")({
  head: () => ({ meta: [{ title: "Team — TrustPanel" }] }),
  component: TeamSettings,
});

const ROLES: WorkspaceRole[] = ["Admin", "Member", "Viewer"];

const roleTone = (r: WorkspaceRole) =>
  r === "Owner" ? "primary" as const : r === "Admin" ? "info" as const : "neutral" as const;

function TeamSettings() {
  useRequireAuth();
  const { data: me } = useMe();
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["team"] });

  const { data: members = [], isLoading } = useQuery({
    queryKey: ["team", me?.workspaceId],
    queryFn: () => teamApi.list(me!.workspaceId!),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const removeMember = useMutation({
    mutationFn: (id: string) => teamApi.remove(id, me!.workspaceId!),
    onSuccess: invalidate,
  });

  const changeRole = useMutation({
    mutationFn: ({ id, role }: { id: string; role: WorkspaceRole }) =>
      teamApi.changeRole(id, me!.workspaceId!, role),
    onSuccess: invalidate,
  });

  // Invite modal state
  const [showInvite, setShowInvite] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState<WorkspaceRole>("Member");
  const [inviteMsg, setInviteMsg] = useState<string | null>(null);

  const invite = useMutation({
    mutationFn: () => teamApi.invite(me!.workspaceId!, inviteEmail, inviteRole),
    onSuccess: () => {
      setInviteMsg("Invitation sent!");
      setInviteEmail("");
      invalidate();
      setTimeout(() => { setShowInvite(false); setInviteMsg(null); }, 1500);
    },
    onError: (e: Error) => setInviteMsg(e.message ?? "Failed to send invitation."),
  });

  const accepted = members.filter((m: TeamMember) => m.accepted);
  const pending = members.filter((m: TeamMember) => !m.accepted);

  return (
    <DashboardLayout
      title="Team"
      action={
        <button className="tp-btn tp-btn-primary" onClick={() => setShowInvite(true)}>
          <Plus size={14} /> Invite member
        </button>
      }
    >
      {showInvite && (
        <div className="tp-card p-6 mb-6 max-w-lg">
          <div className="font-semibold mb-4">Invite a team member</div>
          <div className="space-y-3">
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>Email</label>
              <input
                className="tp-input"
                type="email"
                placeholder="colleague@example.com"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
              />
            </div>
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>Role</label>
              <select className="tp-input" value={inviteRole} onChange={(e) => setInviteRole(e.target.value as WorkspaceRole)}>
                {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
            {inviteMsg && (
              <p className="text-xs" style={{ color: inviteMsg.includes("sent") ? "var(--success)" : "var(--danger)" }}>
                {inviteMsg}
              </p>
            )}
            <div className="flex gap-2">
              <button className="tp-btn tp-btn-primary" onClick={() => invite.mutate()} disabled={invite.isPending || !inviteEmail}>
                {invite.isPending ? "Sending…" : "Send invitation"}
              </button>
              <button className="tp-btn tp-btn-ghost" onClick={() => { setShowInvite(false); setInviteMsg(null); }}>Cancel</button>
            </div>
          </div>
        </div>
      )}

      <div className="tp-card overflow-hidden mb-6">
        <table className="w-full text-sm">
          <thead style={{ background: "var(--surface)" }}>
            <tr>
              {["Member", "Role", "Joined", ""].map((h) => (
                <th key={h} className="px-5 py-3 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr><td colSpan={4} className="px-5 py-4 text-sm" style={{ color: "var(--subtle)" }}>Loading…</td></tr>
            )}
            {accepted.map((m: TeamMember) => (
              <tr key={m.id} style={{ borderTop: "1px solid var(--border)" }}>
                <td className="px-5 py-3">
                  <div className="font-medium">{m.email ?? "—"}</div>
                  {m.userId && <div className="text-xs" style={{ color: "var(--subtle)" }}>ID: {m.userId}</div>}
                </td>
                <td className="px-5 py-3">
                  {m.role === "Owner" ? (
                    <Pill tone={roleTone(m.role)}>{m.role}</Pill>
                  ) : (
                    <select
                      className="text-xs px-2 py-1 rounded"
                      style={{ background: "var(--surface)", border: "1px solid var(--border)", color: "var(--foreground)" }}
                      value={m.role}
                      onChange={(e) => changeRole.mutate({ id: m.id, role: e.target.value as WorkspaceRole })}
                    >
                      {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                    </select>
                  )}
                </td>
                <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                  {new Date(m.createdAt).toLocaleDateString()}
                </td>
                <td className="px-5 py-3 text-right">
                  {m.role !== "Owner" && (
                    <button
                      className="text-xs flex items-center gap-1 ml-auto"
                      style={{ color: "var(--danger)" }}
                      onClick={() => { if (confirm("Remove this member?")) removeMember.mutate(m.id); }}
                      disabled={removeMember.isPending}
                    >
                      <Trash2 size={12} /> Remove
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {pending.length > 0 && (
        <div className="tp-card overflow-hidden">
          <div className="px-5 py-4 border-b" style={{ borderColor: "var(--border)" }}>
            <div className="font-semibold">Pending invitations</div>
          </div>
          <table className="w-full text-sm">
            <tbody>
              {pending.map((m: TeamMember) => (
                <tr key={m.id} style={{ borderTop: "1px solid var(--border)" }}>
                  <td className="px-5 py-3">{m.email}</td>
                  <td className="px-5 py-3"><Pill tone={roleTone(m.role)}>{m.role}</Pill></td>
                  <td className="px-5 py-3" style={{ color: "var(--muted-foreground)" }}>
                    Invited {new Date(m.createdAt).toLocaleDateString()}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      className="text-xs"
                      style={{ color: "var(--subtle)" }}
                      onClick={() => { if (confirm("Cancel this invitation?")) removeMember.mutate(m.id); }}
                    >
                      Cancel
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
