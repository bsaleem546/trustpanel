import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useRef, useState } from "react";
import { Logo } from "@/components/Logo";
import { api } from "@/lib/api/client";

export const Route = createFileRoute("/unsubscribe")({
  head: () => ({ meta: [{ title: "Unsubscribe — TrustPanel" }] }),
  validateSearch: (s: Record<string, unknown>) => ({ token: (s.token as string) ?? "" }),
  component: Unsubscribe,
});

function Unsubscribe() {
  const { token } = Route.useSearch();
  const [state, setState] = useState<"pending" | "success" | "error">("pending");
  const called = useRef(false);

  useEffect(() => {
    if (called.current || !token) { if (!token) setState("error"); return; }
    called.current = true;
    api(`/api/email/unsubscribe?token=${encodeURIComponent(token)}`)
      .then(() => setState("success"))
      .catch(() => setState("error"));
  }, [token]);

  return (
    <div className="min-h-screen flex flex-col items-center justify-center p-6">
      <Logo />
      <div className="mt-10 tp-card p-10 max-w-md w-full text-center">
        {state === "pending" && <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>Processing…</p>}
        {state === "success" && (
          <>
            <div className="text-xl font-semibold mb-2">You've been unsubscribed</div>
            <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>
              You won't receive marketing emails from TrustPanel anymore.
            </p>
          </>
        )}
        {state === "error" && (
          <>
            <div className="text-xl font-semibold mb-2" style={{ color: "var(--danger)" }}>Invalid link</div>
            <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>
              This unsubscribe link is invalid or has already been used.
            </p>
          </>
        )}
      </div>
    </div>
  );
}
