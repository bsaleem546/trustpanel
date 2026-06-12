import { Link } from "@tanstack/react-router";
import { Logo } from "@/components/Logo";

export function MarketingNav() {
  return (
    <header
      className="sticky top-0 z-30 px-6 py-4 flex items-center justify-between"
      style={{ background: "rgba(15,15,20,0.8)", backdropFilter: "blur(10px)", borderBottom: "1px solid var(--border)" }}
    >
      <Link to="/">
        <Logo />
      </Link>
      <nav className="hidden md:flex items-center gap-7 text-sm" style={{ color: "var(--muted-foreground)" }}>
        <Link to="/pricing">Pricing</Link>
        <a href="#features">Features</a>
        <a href="#compare">Compare</a>
        <a href="#faq">FAQ</a>
      </nav>
      <div className="flex items-center gap-2">
        <Link to="/login" className="tp-btn tp-btn-ghost">
          Sign in
        </Link>
        <Link to="/register" className="tp-btn tp-btn-primary">
          Start free trial
        </Link>
      </div>
    </header>
  );
}

export function Footer() {
  return (
    <footer className="px-6 py-12 mt-20 border-t" style={{ borderColor: "var(--border)" }}>
      <div className="max-w-6xl mx-auto flex flex-wrap justify-between gap-8">
        <div>
          <Logo />
          <p className="text-sm mt-3 max-w-xs" style={{ color: "var(--subtle)" }}>
            Collect. Curate. Convert. The testimonial platform agencies actually use.
          </p>
        </div>
        {[
          { title: "Product", links: ["Features", "Pricing", "Widgets", "Changelog"] },
          { title: "Company", links: ["About", "Blog", "Careers", "Contact"] },
          { title: "Resources", links: ["Docs", "API", "Help center", "Status"] },
        ].map((g) => (
          <div key={g.title}>
            <div className="text-sm font-semibold mb-3">{g.title}</div>
            <ul className="space-y-2 text-sm" style={{ color: "var(--muted-foreground)" }}>
              {g.links.map((l) => (
                <li key={l}>
                  <a href="#">{l}</a>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
      <div
        className="max-w-6xl mx-auto mt-10 pt-6 text-xs flex justify-between"
        style={{ borderTop: "1px solid var(--border)", color: "var(--subtle)" }}
      >
        <div>© 2026 TrustPanel. All rights reserved.</div>
        <div className="flex gap-4">
          <a href="#">Privacy</a>
          <a href="#">Terms</a>
        </div>
      </div>
    </footer>
  );
}
