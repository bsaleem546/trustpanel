import { createFileRoute, Link } from "@tanstack/react-router";
import { Check, Minus, Play, Sparkles, Globe, Lock, Layers, Code2, Boxes } from "lucide-react";
import { MarketingNav, Footer } from "@/components/marketing/MarketingNav";
import { Stars, Avatar } from "@/components/Stars";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "TrustPanel — The testimonial platform agencies actually use" },
      { name: "description", content: "Collect video and text testimonials, embed them anywhere, and resell the whole thing under your own brand." },
    ],
  }),
  component: Landing,
});

const features = [
  { icon: Lock, title: "White-label by default", body: "Your domain, your branding, your client's logo on every collection page and widget." },
  { icon: Play, title: "In-browser video recording", body: "Submitters record straight from the browser. No app downloads, no friction." },
  { icon: Sparkles, title: "AI sentiment & insights", body: "Auto-rank quality, surface key themes, and route lukewarm reviews privately." },
  { icon: Layers, title: "Shadow DOM widgets", body: "Embeds that never collide with your host site's CSS, on any stack." },
  { icon: Boxes, title: "Multi-workspace agency mode", body: "One login. Unlimited client workspaces. Bill them however you want." },
  { icon: Code2, title: "Public REST API", body: "Build custom flows on top — fetch, post, and webhook every event." },
];

const compareRows = [
  ["Testimonial.to", "$45/mo", "✓", "Add-on", "—", "—"],
  ["Senja.io", "$29/mo", "✓", "Pro plan", "Basic", "—"],
  ["StoryPrompt", "$49/mo", "✓", "—", "—", "—"],
  ["Endorsal", "$39/mo", "—", "—", "—", "—"],
  ["TrustPanel", "$29/mo", "✓", "All plans", "Advanced", "✓"],
];

const plans = [
  { name: "Starter", price: 29, features: ["1 workspace", "100 testimonials", "3 widgets", "Text testimonials", "Email support"], excl: ["Video", "AI insights", "White-label"] },
  { name: "Pro", price: 59, features: ["1 workspace", "Unlimited testimonials", "10 widgets", "Video recording", "AI sentiment"], excl: ["Agency mode", "White-label"] },
  { name: "Agency", price: 119, featured: true, features: ["10 workspaces", "Unlimited everything", "Unlimited widgets", "White-label domain", "AI insights", "Priority support"], excl: ["Dedicated CSM"] },
  { name: "Agency+", price: 199, features: ["Unlimited workspaces", "Custom email sender", "SSO + audit logs", "Dedicated CSM", "99.99% SLA"], excl: [] },
];

function Landing() {
  return (
    <div>
      <MarketingNav />
      {/* Hero */}
      <section className="px-6 pt-20 pb-24 max-w-6xl mx-auto grid lg:grid-cols-2 gap-12 items-center">
        <div>
          <div className="tp-pill mb-5" style={{ background: "var(--primary-soft)", color: "var(--primary-light)" }}>
            <span className="w-1.5 h-1.5 rounded-full" style={{ background: "var(--primary-light)" }} /> NEW · Multi-workspace agency mode
          </div>
          <h1 className="text-5xl md:text-6xl font-semibold tracking-tight leading-[1.05]">
            The testimonial platform <span style={{ color: "var(--primary-light)" }}>agencies actually use</span>
          </h1>
          <p className="mt-5 text-lg" style={{ color: "var(--muted-foreground)" }}>
            Collect video and text testimonials, embed them anywhere, and resell the whole thing under your own brand.
          </p>
          <div className="flex gap-3 mt-7">
            <Link to="/register" className="tp-btn tp-btn-primary" style={{ padding: "12px 20px" }}>
              Start free trial
            </Link>
            <a href="#demo" className="tp-btn tp-btn-ghost" style={{ padding: "12px 20px" }}>
              See a demo
            </a>
          </div>
          <div className="mt-6 text-xs" style={{ color: "var(--subtle)" }}>
            14-day trial · No card required · Cancel anytime
          </div>
        </div>
        <DashboardMockup />
      </section>

      {/* Logos */}
      <section className="px-6 py-10 border-y" style={{ borderColor: "var(--border)" }}>
        <div className="max-w-6xl mx-auto flex flex-wrap items-center justify-center gap-x-12 gap-y-4 text-sm" style={{ color: "var(--subtle)" }}>
          <span className="text-xs uppercase tracking-wider mr-4">Integrates with</span>
          {["Webflow", "Framer", "WordPress", "Shopify", "Wix", "Notion"].map((b) => (
            <span key={b} className="font-medium text-base" style={{ color: "var(--muted-foreground)" }}>
              {b}
            </span>
          ))}
        </div>
      </section>

      {/* Features */}
      <section id="features" className="px-6 py-24 max-w-6xl mx-auto">
        <div className="text-center mb-14 max-w-2xl mx-auto">
          <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--primary-light)" }}>
            Features
          </div>
          <h2 className="text-3xl md:text-4xl font-semibold tracking-tight">Everything you need to turn happy customers into proof</h2>
        </div>
        <div className="grid md:grid-cols-3 gap-4">
          {features.map(({ icon: Icon, title, body }) => (
            <div key={title} className="tp-card p-6">
              <div className="w-10 h-10 rounded-lg flex items-center justify-center mb-4" style={{ background: "var(--primary-soft)" }}>
                <Icon size={18} style={{ color: "var(--primary-light)" }} />
              </div>
              <h3 className="font-semibold mb-2">{title}</h3>
              <p className="text-sm" style={{ color: "var(--muted-foreground)" }}>
                {body}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* Compare */}
      <section id="compare" className="px-6 py-20 max-w-6xl mx-auto">
        <h2 className="text-3xl font-semibold tracking-tight text-center mb-10">How we compare</h2>
        <div className="tp-card overflow-hidden">
          <table className="w-full text-sm">
            <thead style={{ background: "var(--surface)" }}>
              <tr className="text-left">
                {["Tool", "Starting price", "Video", "White-label", "AI", "Agency mode"].map((h) => (
                  <th key={h} className="px-5 py-4 font-medium" style={{ color: "var(--muted-foreground)" }}>
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {compareRows.map((r) => {
                const highlighted = r[0] === "TrustPanel";
                return (
                  <tr
                    key={r[0]}
                    style={{
                      background: highlighted ? "var(--primary-soft)" : "transparent",
                      borderTop: "1px solid var(--border)",
                    }}
                  >
                    {r.map((c, i) => (
                      <td
                        key={i}
                        className="px-5 py-4"
                        style={{ fontWeight: highlighted && i === 0 ? 600 : 400, color: highlighted ? "var(--primary-light)" : undefined }}
                      >
                        {c}
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      {/* Pricing */}
      <section id="pricing" className="px-6 py-20 max-w-6xl mx-auto">
        <div className="text-center mb-12">
          <h2 className="text-3xl md:text-4xl font-semibold tracking-tight mb-3">Pricing that scales with you</h2>
          <p style={{ color: "var(--muted-foreground)" }}>Bill monthly, or save 20% annually.</p>
        </div>
        <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-4">
          {plans.map((p) => (
            <div
              key={p.name}
              className="tp-card p-6"
              style={p.featured ? { border: "1.5px solid var(--primary)", position: "relative" } : undefined}
            >
              {p.featured && (
                <div
                  className="absolute -top-3 left-6 tp-pill"
                  style={{ background: "var(--primary)", color: "white" }}
                >
                  MOST POPULAR
                </div>
              )}
              <div className="font-semibold">{p.name}</div>
              <div className="mt-3 flex items-baseline gap-1">
                <span className="text-4xl font-semibold">${p.price}</span>
                <span style={{ color: "var(--subtle)" }}>/mo</span>
              </div>
              <Link to="/register" className={`tp-btn ${p.featured ? "tp-btn-primary" : "tp-btn-ghost"} w-full mt-5`}>
                Start free trial
              </Link>
              <div className="mt-6 space-y-2.5 text-sm">
                {p.features.map((f) => (
                  <div key={f} className="flex gap-2">
                    <Check size={16} style={{ color: "var(--success)" }} />
                    <span>{f}</span>
                  </div>
                ))}
                {p.excl.map((f) => (
                  <div key={f} className="flex gap-2" style={{ color: "var(--subtle)" }}>
                    <Minus size={16} />
                    <span>{f}</span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </section>

      <Footer />
    </div>
  );
}

function DashboardMockup() {
  return (
    <div className="tp-card p-4" style={{ background: "var(--surface)" }}>
      <div className="flex items-center gap-1.5 mb-4">
        <span className="w-2.5 h-2.5 rounded-full" style={{ background: "#f87171" }} />
        <span className="w-2.5 h-2.5 rounded-full" style={{ background: "#fbbf24" }} />
        <span className="w-2.5 h-2.5 rounded-full" style={{ background: "#34d399" }} />
      </div>
      <div className="space-y-3">
        {[
          { name: "Sarah Kowalski", company: "Northwind Studios", rating: 5, text: "TrustPanel cut our review collection workflow from days to minutes.", color: "#7c6af7" },
          { name: "Marco Bertelli", company: "Pixelhaus", rating: 5, text: "We doubled our testimonial collection rate the first month.", color: "#34d399" },
          { name: "Priya Raman", company: "Loomwise", rating: 4, text: "Zero CSS conflicts when embedding across three CMS platforms.", color: "#60a5fa" },
        ].map((t) => (
          <div key={t.name} className="tp-card p-4 flex gap-3 items-start" style={{ background: "var(--card)" }}>
            <Avatar name={t.name} color={t.color} />
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-sm font-medium">{t.name}</div>
                  <div className="text-xs" style={{ color: "var(--subtle)" }}>
                    {t.company}
                  </div>
                </div>
                <Stars value={t.rating} />
              </div>
              <p className="text-sm mt-2" style={{ color: "var(--muted-foreground)" }}>
                "{t.text}"
              </p>
            </div>
          </div>
        ))}
      </div>
      <div className="mt-4 flex items-center justify-between text-xs px-2" style={{ color: "var(--subtle)" }}>
        <span className="flex items-center gap-1.5">
          <Globe size={12} /> Embedded on pricing.northwind.studio
        </span>
        <span>1,284 views today</span>
      </div>
    </div>
  );
}
