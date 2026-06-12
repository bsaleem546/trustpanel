import { useMutation } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { MailCheck } from "lucide-react";
import { Logo } from "@/components/Logo";
import { authApi, googleSignInUrl } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

export const Route = createFileRoute("/register")({
  head: () => ({ meta: [{ title: "Create account — TrustPanel" }] }),
  component: Register,
});

function Register() {
  const [workspaceName, setWorkspaceName] = useState("");
  const [email, setEmail] = useState("");
  const [pw, setPw] = useState("");
  const strength = Math.min(4, Math.floor(pw.length / 3));
  const labels = ["Weak", "Okay", "Good", "Strong", "Excellent"];
  const colors = ["#f87171", "#fbbf24", "#fbbf24", "#34d399", "#34d399"];

  const register = useMutation({
    mutationFn: () =>
      authApi.register({
        email,
        password: pw,
        workspaceName: workspaceName.trim() || undefined,
      }),
  });

  const fieldErrors =
    register.error instanceof ApiError ? Object.values(register.error.fieldErrors).flat() : [];
  const errorMessage =
    register.error instanceof ApiError
      ? fieldErrors[0] ?? register.error.message
      : register.error
        ? "Could not reach the server. Is the API running?"
        : null;

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-sm">
          <Logo />
          {register.isSuccess ? (
            <div className="mt-8">
              <MailCheck size={32} style={{ color: "var(--success)" }} />
              <h1 className="text-2xl font-semibold tracking-tight mt-4">Check your email</h1>
              <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
                We sent a confirmation link to <span className="font-medium">{email}</span>. Confirm
                your address, then sign in.
              </p>
              <Link to="/login" className="tp-btn tp-btn-primary w-full mt-6">
                Go to sign in
              </Link>
            </div>
          ) : (
            <>
              <h1 className="text-2xl font-semibold tracking-tight mt-8">Create your account</h1>
              <p className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
                14-day free trial. No card needed.
              </p>
              <form
                className="mt-7 space-y-4"
                onSubmit={(e) => {
                  e.preventDefault();
                  if (!register.isPending) register.mutate();
                }}
              >
                <div>
                  <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                    Workspace name
                  </label>
                  <input
                    className="tp-input"
                    placeholder="Northwind Agency"
                    value={workspaceName}
                    onChange={(e) => setWorkspaceName(e.target.value)}
                  />
                </div>
                <div>
                  <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                    Work email
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
                  <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                    Password
                  </label>
                  <input
                    className="tp-input"
                    type="password"
                    value={pw}
                    onChange={(e) => setPw(e.target.value)}
                    required
                    minLength={8}
                  />
                  <div className="flex gap-1 mt-2">
                    {[0, 1, 2, 3].map((i) => (
                      <div key={i} className="flex-1 h-1 rounded-full" style={{ background: i < strength ? colors[strength] : "var(--border)" }} />
                    ))}
                  </div>
                  <div className="text-xs mt-1.5" style={{ color: "var(--subtle)" }}>
                    Strength: {labels[strength]}
                  </div>
                </div>
                <label className="flex items-start gap-2 text-xs" style={{ color: "var(--muted-foreground)" }}>
                  <input type="checkbox" defaultChecked className="mt-0.5" required />I agree to the Terms of Service and Privacy Policy.
                </label>
                {errorMessage && (
                  <p className="text-sm" style={{ color: "var(--danger)" }}>
                    {errorMessage}
                  </p>
                )}
                <button type="submit" className="tp-btn tp-btn-primary w-full" disabled={register.isPending}>
                  {register.isPending ? "Creating account…" : "Create account"}
                </button>
              </form>
              <div className="flex items-center gap-3 my-6 text-xs" style={{ color: "var(--subtle)" }}>
                <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
                or
                <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
              </div>
              <a href={googleSignInUrl} className="tp-btn tp-btn-ghost w-full">
                <span className="w-4 h-4 rounded-full" style={{ background: "white" }} /> Continue with Google
              </a>
              <p className="text-center text-sm mt-7" style={{ color: "var(--muted-foreground)" }}>
                Already have an account?{" "}
                <Link to="/login" style={{ color: "var(--primary-light)" }}>
                  Sign in
                </Link>
              </p>
            </>
          )}
        </div>
      </div>
      <div className="hidden lg:block" style={{ background: "var(--surface)", borderLeft: "1px solid var(--border)" }} />
    </div>
  );
}
