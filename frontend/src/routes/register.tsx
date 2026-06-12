import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { Logo } from "@/components/Logo";

export const Route = createFileRoute("/register")({
  head: () => ({ meta: [{ title: "Create account — TrustPanel" }] }),
  component: Register,
});

function Register() {
  const [pw, setPw] = useState("Northwind24");
  const strength = Math.min(4, Math.floor(pw.length / 3));
  const labels = ["Weak", "Okay", "Good", "Strong", "Excellent"];
  const colors = ["#f87171", "#fbbf24", "#fbbf24", "#34d399", "#34d399"];

  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-sm">
          <Logo />
          <h1 className="text-2xl font-semibold tracking-tight mt-8">Create your account</h1>
          <p className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
            14-day free trial. No card needed.
          </p>
          <form className="mt-7 space-y-4">
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Full name
              </label>
              <input className="tp-input" placeholder="Alex Mendez" />
            </div>
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Work email
              </label>
              <input className="tp-input" placeholder="you@company.com" />
            </div>
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Password
              </label>
              <input className="tp-input" type="password" value={pw} onChange={(e) => setPw(e.target.value)} />
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
              <input type="checkbox" defaultChecked className="mt-0.5" />I agree to the Terms of Service and Privacy Policy.
            </label>
            <Link to="/onboarding" className="tp-btn tp-btn-primary w-full">
              Create account
            </Link>
          </form>
          <div className="flex items-center gap-3 my-6 text-xs" style={{ color: "var(--subtle)" }}>
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
            or
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
          </div>
          <button className="tp-btn tp-btn-ghost w-full">
            <span className="w-4 h-4 rounded-full" style={{ background: "white" }} /> Continue with Google
          </button>
          <p className="text-center text-sm mt-7" style={{ color: "var(--muted-foreground)" }}>
            Already have an account?{" "}
            <Link to="/login" style={{ color: "var(--primary-light)" }}>
              Sign in
            </Link>
          </p>
        </div>
      </div>
      <div className="hidden lg:block" style={{ background: "var(--surface)", borderLeft: "1px solid var(--border)" }} />
    </div>
  );
}
