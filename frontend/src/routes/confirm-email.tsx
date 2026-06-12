import { useMutation } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useEffect } from "react";
import { Logo } from "@/components/Logo";
import { authApi } from "@/lib/api/auth";
import { ApiError } from "@/lib/api/client";

type ConfirmEmailSearch = { userId?: string; token?: string };

export const Route = createFileRoute("/confirm-email")({
  head: () => ({ meta: [{ title: "Confirm email — TrustPanel" }] }),
  validateSearch: (search: Record<string, unknown>): ConfirmEmailSearch => ({
    userId: typeof search.userId === "string" ? search.userId : undefined,
    token: typeof search.token === "string" ? search.token : undefined,
  }),
  component: ConfirmEmail,
});

function ConfirmEmail() {
  const { userId, token } = Route.useSearch();

  const confirm = useMutation({
    mutationFn: () => authApi.confirmEmail({ userId: userId!, token: token! }),
  });
  const { mutate } = confirm;

  useEffect(() => {
    if (userId && token) mutate();
  }, [userId, token, mutate]);

  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-sm text-center">
        <div className="flex justify-center">
          <Logo />
        </div>
        {!userId || !token ? (
          <p className="text-sm mt-6" style={{ color: "var(--muted-foreground)" }}>
            This confirmation link is incomplete. Open the link from your email, or request a new
            one by signing in.
          </p>
        ) : confirm.isPending || confirm.isIdle ? (
          <p className="text-sm mt-6" style={{ color: "var(--muted-foreground)" }}>
            Confirming your email…
          </p>
        ) : confirm.isSuccess ? (
          <>
            <h1 className="text-2xl font-semibold tracking-tight mt-6">Email confirmed</h1>
            <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
              Your address is verified. You can sign in now.
            </p>
            <Link to="/login" className="tp-btn tp-btn-primary w-full mt-6">
              Sign in
            </Link>
          </>
        ) : (
          <>
            <h1 className="text-2xl font-semibold tracking-tight mt-6">Confirmation failed</h1>
            <p className="text-sm mt-2" style={{ color: "var(--danger)" }}>
              {confirm.error instanceof ApiError
                ? confirm.error.message
                : "Could not reach the server. Is the API running?"}
            </p>
            <Link to="/login" className="tp-btn tp-btn-ghost w-full mt-6">
              Back to sign in
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
