import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useRef, useState } from "react";
import { Logo } from "@/components/Logo";
import { authApi } from "@/lib/api/auth";

// Landing page for the Google OAuth flow. The backend redirects here after
// setting the httpOnly refresh cookie; we exchange it for an access token.
export const Route = createFileRoute("/auth/callback")({
  head: () => ({ meta: [{ title: "Signing in — TrustPanel" }] }),
  component: AuthCallback,
});

function AuthCallback() {
  const navigate = useNavigate();
  const [failed, setFailed] = useState(false);
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    authApi
      .refreshSession()
      .then((payload) => {
        navigate({ to: payload.user.onboardingCompleted ? "/dashboard" : "/onboarding" });
      })
      .catch(() => setFailed(true));
  }, [navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-sm text-center">
        <div className="flex justify-center">
          <Logo />
        </div>
        {failed ? (
          <>
            <p className="text-sm mt-6" style={{ color: "var(--danger)" }}>
              Google sign-in didn't complete. Please try again.
            </p>
            <button className="tp-btn tp-btn-ghost w-full mt-4" onClick={() => navigate({ to: "/login" })}>
              Back to sign in
            </button>
          </>
        ) : (
          <p className="text-sm mt-6" style={{ color: "var(--muted-foreground)" }}>
            Finishing sign-in…
          </p>
        )}
      </div>
    </div>
  );
}
