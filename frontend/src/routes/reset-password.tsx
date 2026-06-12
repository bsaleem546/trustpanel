import { useMutation } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { Logo } from "@/components/Logo";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

type ResetPasswordSearch = { email?: string; token?: string };

export const Route = createFileRoute("/reset-password")({
  head: () => ({ meta: [{ title: "Choose a new password — TrustPanel" }] }),
  validateSearch: (search: Record<string, unknown>): ResetPasswordSearch => ({
    email: typeof search.email === "string" ? search.email : undefined,
    token: typeof search.token === "string" ? search.token : undefined,
  }),
  component: ResetPassword,
});

function ResetPassword() {
  const { email: emailFromLink, token } = Route.useSearch();
  const [email, setEmail] = useState(emailFromLink ?? "");
  const [password, setPassword] = useState("");

  const reset = useMutation({
    mutationFn: () => authApi.resetPassword({ email, token: token ?? "", newPassword: password }),
  });

  if (!token) {
    return (
      <Shell>
        <p className="text-sm mt-6" style={{ color: "var(--muted-foreground)" }}>
          This reset link is incomplete. Open the link from your email, or request a new one.
        </p>
        <Link to="/forgot-password" className="tp-btn tp-btn-primary w-full mt-6">
          Request a new link
        </Link>
      </Shell>
    );
  }

  if (reset.isSuccess) {
    return (
      <Shell>
        <h1 className="text-2xl font-semibold tracking-tight mt-6">Password updated</h1>
        <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
          Sign in with your new password.
        </p>
        <Link to="/login" className="tp-btn tp-btn-primary w-full mt-6">
          Sign in
        </Link>
      </Shell>
    );
  }

  return (
    <Shell>
      <h1 className="text-2xl font-semibold tracking-tight mt-6">Choose a new password</h1>
      <form
        className="mt-7 space-y-4 text-left"
        onSubmit={(e) => {
          e.preventDefault();
          if (!reset.isPending) reset.mutate();
        }}
      >
        <div>
          <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
            Email
          </label>
          <input
            className="tp-input"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>
        <div>
          <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
            New password
          </label>
          <input
            className="tp-input"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
          />
        </div>
        {reset.isError && (
          <p className="text-sm" style={{ color: "var(--danger)" }}>
            {reset.error instanceof ApiError
              ? reset.error.message
              : "Could not reach the server. Is the API running?"}
          </p>
        )}
        <button type="submit" className="tp-btn tp-btn-primary w-full" disabled={reset.isPending}>
          {reset.isPending ? "Updating…" : "Update password"}
        </button>
      </form>
    </Shell>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-sm text-center">
        <div className="flex justify-center">
          <Logo />
        </div>
        {children}
      </div>
    </div>
  );
}
