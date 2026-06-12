import { Link, useRouterState } from "@tanstack/react-router";
import {
  LayoutDashboard,
  Inbox,
  FileText,
  Send,
  LayoutGrid,
  Heart,
  BarChart3,
  Sparkles,
  Download,
  Settings,
  Users,
  CreditCard,
  KeyRound,
  Building2,
} from "lucide-react";
import { Logo } from "@/components/Logo";
import { Pill } from "@/components/Stars";

type NavItem = { to: string; label: string; icon: typeof LayoutDashboard; exact?: boolean; badge?: number };
const groups: { label: string; items: NavItem[] }[] = [
  {
    label: "Overview",
    items: [{ to: "/dashboard", label: "Dashboard", icon: LayoutDashboard, exact: true }],
  },
  {
    label: "Collect",
    items: [
      { to: "/dashboard/forms", label: "Forms", icon: FileText },
      { to: "/dashboard/requests", label: "Requests", icon: Send },
    ],
  },
  {
    label: "Manage",
    items: [
      { to: "/dashboard/testimonials", label: "Testimonials", icon: Inbox, badge: 12 },
      { to: "/dashboard/imports", label: "Imports", icon: Download },
    ],
  },
  {
    label: "Display",
    items: [
      { to: "/dashboard/widgets/create", label: "Widgets", icon: LayoutGrid },
      { to: "/dashboard/wall", label: "Wall of Love", icon: Heart },
    ],
  },
  {
    label: "Grow",
    items: [
      { to: "/dashboard/analytics", label: "Analytics", icon: BarChart3 },
      { to: "/dashboard/insights", label: "AI Insights", icon: Sparkles },
    ],
  },
  {
    label: "Settings",
    items: [
      { to: "/dashboard/settings/workspace", label: "Workspace", icon: Building2 },
      { to: "/dashboard/settings/team", label: "Team", icon: Users },
      { to: "/dashboard/settings/billing", label: "Billing", icon: CreditCard },
      { to: "/dashboard/settings/api-keys", label: "API Keys", icon: KeyRound },
    ],
  },
];

export function Sidebar() {
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  return (
    <aside
      className="hidden md:flex flex-col shrink-0"
      style={{
        width: 260,
        background: "var(--surface)",
        borderRight: "1px solid var(--border)",
        height: "100vh",
        position: "sticky",
        top: 0,
      }}
    >
      <div className="px-5 py-5 border-b" style={{ borderColor: "var(--border)" }}>
        <Logo />
      </div>
      <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-5">
        {groups.map((g) => (
          <div key={g.label}>
            <div
              className="px-3 mb-1 text-[10px] font-semibold uppercase tracking-wider"
              style={{ color: "var(--subtle)" }}
            >
              {g.label}
            </div>
            <div className="space-y-0.5">
              {g.items.map((it) => {
                const active = it.exact ? pathname === it.to : pathname === it.to || pathname.startsWith(it.to + "/");
                const Icon = it.icon;
                return (
                  <Link
                    key={it.to}
                    to={it.to}
                    className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors"
                    style={{
                      background: active ? "var(--primary-soft)" : "transparent",
                      color: active ? "var(--primary-light)" : "var(--muted-foreground)",
                    }}
                  >
                    <Icon size={16} strokeWidth={1.75} />
                    <span className="flex-1">{it.label}</span>
                    {"badge" in it && it.badge ? <Pill tone="primary">{it.badge}</Pill> : null}
                  </Link>
                );
              })}
            </div>
          </div>
        ))}
      </nav>
      <div className="p-3 border-t" style={{ borderColor: "var(--border)" }}>
        <div className="flex items-center gap-2.5 p-2 rounded-lg" style={{ background: "var(--card)" }}>
          <div
            className="w-9 h-9 rounded-full flex items-center justify-center font-medium"
            style={{ background: "rgba(124,106,247,0.18)", color: "var(--primary-light)" }}
          >
            AM
          </div>
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium truncate">Alex Mendez</div>
            <div className="text-xs" style={{ color: "var(--subtle)" }}>
              Agency plan
            </div>
          </div>
          <Settings size={15} style={{ color: "var(--subtle)" }} />
        </div>
      </div>
    </aside>
  );
}
