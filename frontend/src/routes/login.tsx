import { createFileRoute, Link } from "@tanstack/react-router";
import { Logo } from "@/components/Logo";

export const Route = createFileRoute("/login")({
  head: () => ({ meta: [{ title: "Sign in — TrustPanel" }] }),
  component: Login,
});

function Login() {
  return (
    <div className="min-h-screen grid lg:grid-cols-2">
      <div className="flex items-center justify-center p-8">
        <div className="w-full max-w-sm">
          <Logo />
          <h1 className="text-2xl font-semibold tracking-tight mt-8">Sign in to your account</h1>
          <p className="text-sm mt-1" style={{ color: "var(--muted-foreground)" }}>
            Welcome back. Let's collect some social proof.
          </p>
          <form className="mt-7 space-y-4">
            <div>
              <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
                Email
              </label>
              <input className="tp-input" placeholder="you@company.com" defaultValue="alex@northwind.agency" />
            </div>
            <div>
              <div className="flex justify-between mb-1.5">
                <label className="text-xs font-medium" style={{ color: "var(--muted-foreground)" }}>
                  Password
                </label>
                <a href="#" className="text-xs" style={{ color: "var(--primary-light)" }}>
                  Forgot password?
                </a>
              </div>
              <input className="tp-input" type="password" placeholder="••••••••" defaultValue="password" />
            </div>
            <Link to="/dashboard" className="tp-btn tp-btn-primary w-full">
              Sign in
            </Link>
          </form>
          <div className="flex items-center gap-3 my-6 text-xs" style={{ color: "var(--subtle)" }}>
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
            or continue with
            <div className="flex-1 h-px" style={{ background: "var(--border)" }} />
          </div>
          <button className="tp-btn tp-btn-ghost w-full">
            <span className="w-4 h-4 rounded-full" style={{ background: "white" }} /> Continue with Google
          </button>
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
