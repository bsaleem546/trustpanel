import { useMutation } from "@tanstack/react-query";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { Logo } from "@/components/Logo";
import { authApi, googleSignInUrl } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export const Route = createFileRoute("/login")({
  head: () => ({ meta: [{ title: "Sign in — TrustPanel" }] }),
  component: Login,
});

function Login() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const login = useMutation({
    mutationFn: () => authApi.login({ email, password }),
    onSuccess: (payload) => {
      navigate({ to: payload.user.onboardingCompleted ? "/dashboard" : "/onboarding" });
    },
  });

  const errorMessage =
    login.error instanceof ApiError
      ? login.error.message
      : login.error
        ? "Could not reach the server. Is the API running?"
        : null;

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-sm">
          <Logo />
          <h1 className="text-2xl font-semibold tracking-tight mt-8">Sign in to your account</h1>
          <p className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
            Welcome back. Let's collect some social proof.
          </p>
          <form
            className="mt-7 space-y-4"
            onSubmit={(e) => {
              e.preventDefault();
              if (!login.isPending) login.mutate();
            }}
          >
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Email
              </label>
              <input
                className="tp-input"
                type="email"
                placeholder="you@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div>
              <div className="flex justify-between mb-1.5">
                <label className="text-xs font-medium" style={{ color: "var(--muted-foreground)" }}>
                  Password
                </label>
                <Link to="/forgot-password" className="text-xs" style={{ color: "var(--primary-light)" }}>
                  Forgot password?
                </Link>
              </div>
              <input
                className="tp-input"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            {errorMessage && (
              <p className="text-sm" style={{ color: "var(--danger)" }}>
                {errorMessage}
              </p>
            )}
            <button type="submit" className="tp-btn tp-btn-primary w-full" disabled={login.isPending}>
              {login.isPending ? "Signing in…" : "Sign in"}
            </button>
          </form>
          <div className="flex items-center gap-3 my-6 text-xs" style={{ color: "var(--subtle)" }}>
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
            or continue with
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
          </div>
          <a href={googleSignInUrl} className="tp-btn tp-btn-ghost w-full">
            <span className="w-4 h-4 rounded-full" style={{ background: "white" }} /> Continue with Google
          </a>
          <p className="text-center text-sm mt-7" style={{ color: "var(--muted-foreground)" }}>
            Don't have an account?{" "}
            <Link to="/register" style={{ color: "var(--primary-light)" }}>
              Start free trial
            </Link>
          </p>
        </div>
      </div>
      <div className="hidden lg:flex items-center justify-center p-12 relative overflow-hidden" style={{ background: "var(--surface)", borderLeft: "1px solid var(--border)" }}>
        <div className="tp-card p-6 w-full max-w-md" style={{ background: "var(--card)" }}>
          <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--primary-light)" }}>
            Live dashboard
          </div>
          <div className="text-lg font-medium">247 testimonials collected this quarter</div>
          <div className="grid grid-cols-3 gap-3 mt-5">
            {[
              { l: "Approved", v: "189" },
              { l: "Featured", v: "32" },
              { l: "Pending", v: "12" },
            ].map((s) => (
              <div key={s.l} className="tp-card p-3" style={{ background: "var(--surface)" }}>
                <div className="text-2xl font-semibold">{s.v}</div>
                <div className="text-xs" style={{ color: "var(--subtle)" }}>
                  {s.l}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
