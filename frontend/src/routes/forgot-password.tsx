import { useMutation } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { Logo } from "@/components/Logo";
import { authApi } from "@/lib/api/auth";

export const Route = createFileRoute("/forgot-password")({
  head: () => ({ meta: [{ title: "Reset password — TrustPanel" }] }),
  component: ForgotPassword,
});

function ForgotPassword() {
  const [email, setEmail] = useState("");
  const forgot = useMutation({ mutationFn: () => authApi.forgotPassword(email) });

  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-sm">
        <Logo />
        <h1 className="text-2xl font-semibold tracking-tight mt-8">Reset your password</h1>
        {forgot.isSuccess ? (
          <>
            <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
              If an account exists for <span className="font-medium">{email}</span>, a password
              reset link is on its way.
            </p>
            <Link to="/login" className="tp-btn tp-btn-primary w-full mt-6">
              Back to sign in
            </Link>
          </>
        ) : (
          <>
            <p className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
              Enter your email and we'll send you a reset link.
            </p>
            <form
              className="mt-7 space-y-4"
              onSubmit={(e) => {
                e.preventDefault();
                if (!forgot.isPending) forgot.mutate();
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
              {forgot.isError && (
                <p className="text-sm" style={{ color: "var(--danger)" }}>
                  Could not reach the server. Is the API running?
                </p>
              )}
              <button type="submit" className="tp-btn tp-btn-primary w-full" disabled={forgot.isPending}>
                {forgot.isPending ? "Sending…" : "Send reset link"}
              </button>
            </form>
            <p className="text-center text-sm mt-7" style={{ color: "var(--muted-foreground)" }}>
              Remembered it?{" "}
              <Link to="/login" style={{ color: "var(--primary-light)" }}>
                Sign in
              </Link>
            </p>
          </>
        )}
      </div>
    </div>
  );
}
