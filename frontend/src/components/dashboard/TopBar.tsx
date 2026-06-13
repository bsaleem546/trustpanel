import { Bell, ChevronDown, Search } from "lucide-react";
import { useCurrentWorkspace, useMe, useLogout } from "@/lib/auth";

export function TopBar() {
  const { data: workspace } = useCurrentWorkspace();
  const { data: me } = useMe();
  const logout = useLogout();

  const workspaceName = workspace?.name ?? "…";
  const initials = me?.email
    ? me.email.slice(0, 2).toUpperCase()
    : "…";

  return (
    <div
      className="flex items-center justify-between px-6 h-14 border-b sticky top-0 z-10"
      style={{ borderColor: "var(--border)", background: "rgba(15,15,20,0.85)", backdropFilter: "blur(8px)" }}
    >
      <button className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm" style={{ border: "1px solid var(--border)" }}>
        <div className="w-5 h-5 rounded" style={{ background: "var(--primary)" }} />
        <span>{workspaceName}</span>
        <ChevronDown size={14} style={{ color: "var(--subtle)" }} />
      </button>
      <div className="flex-1 max-w-md mx-6 hidden md:block">
        <div className="relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2" style={{ color: "var(--subtle)" }} />
          <input className="tp-input pl-9" style={{ height: 34 }} placeholder="Search testimonials, forms, widgets…" />
        </div>
      </div>
      <div className="flex items-center gap-3">
        <button className="relative w-8 h-8 rounded-lg flex items-center justify-center" style={{ border: "1px solid var(--border)" }}>
          <Bell size={14} style={{ color: "var(--muted-foreground)" }} />
          <span className="absolute -top-1 -right-1 w-2 h-2 rounded-full" style={{ background: "var(--danger)" }} />
        </button>
        <button
          onClick={logout}
          className="w-8 h-8 rounded-full flex items-center justify-center font-medium text-xs"
          style={{ background: "rgba(124,106,247,0.18)", color: "var(--primary-light)" }}
          title="Sign out"
        >
          {initials}
        </button>
      </div>
    </div>
  );
}
