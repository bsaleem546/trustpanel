import type { ReactNode } from "react";
import { Sidebar } from "./Sidebar";
import { TopBar } from "./TopBar";

export function DashboardLayout({ children, title, action }: { children: ReactNode; title?: string; action?: ReactNode }) {
  return (
    <div className="flex min-h-screen w-full" style={{ background: "var(--background)" }}>
      <Sidebar />
      <div className="flex-1 min-w-0 flex flex-col">
        <TopBar />
        <main className="flex-1 p-6 md:p-8 max-w-[1400px] w-full mx-auto">
          {(title || action) && (
            <div className="flex items-center justify-between mb-6 flex-wrap gap-3">
              <h1 className="tp-h1">{title}</h1>
              <div className="flex items-center gap-2">{action}</div>
            </div>
          )}
          {children}
        </main>
      </div>
    </div>
  );
}
