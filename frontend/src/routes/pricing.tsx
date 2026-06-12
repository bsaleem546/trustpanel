import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { Check, Minus, ChevronDown } from "lucide-react";
import { MarketingNav, Footer } from "@/components/marketing/MarketingNav";

export const Route = createFileRoute("/pricing")({
  head: () => ({ meta: [{ title: "Pricing — TrustPanel" }, { name: "description", content: "Simple, scalable pricing for solo creators to agencies." }] }),
  component: Pricing,
});

const plans = [
  { name: "Starter", priceM: 29, priceA: 23 },
  { name: "Pro", priceM: 59, priceA: 47 },
  { name: "Agency", priceM: 119, priceA: 95, featured: true },
  { name: "Agency+", priceM: 199, priceA: 159 },
];

const rows: { label: string; v: (string | boolean)[] }[] = [
  { label: "Workspaces", v: ["1", "1", "10", "Unlimited"] },
  { label: "Testimonials", v: ["100", "Unlimited", "Unlimited", "Unlimited"] },
  { label: "Widgets", v: ["3", "10", "Unlimited", "Unlimited"] },
  { label: "Video recording", v: [false, true, true, true] },
  { label: "AI sentiment", v: [false, true, true, true] },
  { label: "AI insights", v: [false, false, true, true] },
  { label: "White-label", v: [false, false, true, true] },
  { label: "Custom domain", v: [false, false, true, true] },
  { label: "Custom email sender", v: [false, false, false, true] },
  { label: "Team members", v: ["1", "3", "10", "Unlimited"] },
  { label: "API access", v: [false, true, true, true] },
  { label: "Webhooks", v: [false, true, true, true] },
  { label: "Import sources", v: ["1", "3", "All", "All"] },
  { label: "Priority support", v: [false, false, true, true] },
];

const faqs = [
  { q: "Can I white-label this for my clients?", a: "Yes. All Agency plans include full white-labeling: custom domain, sender email, removed TrustPanel branding, and per-workspace theming." },
  { q: "What counts as a workspace?", a: "A workspace is an isolated environment for one brand or client. Each has its own testimonials, widgets, forms, and team." },
  { q: "Do you store video files?", a: "Yes — videos are stored on global CDN with transcoding for web playback. Pro plan and above includes unlimited video storage." },
  { q: "Can I import existing reviews?", a: "From Google Business Profile, Twitter/X, G2, Capterra, Trustpilot, and via CSV upload." },
  { q: "What happens if I downgrade?", a: "Your data stays. Features above the new plan are read-only until you upgrade again. No data loss." },
];

function Pricing() {
  const [annual, setAnnual] = useState(true);
  return (
    <div>
      <MarketingNav />
      <section className="px-6 pt-20 pb-12 max-w-6xl mx-auto text-center">
        <h1 className="text-4xl md:text-5xl font-semibold tracking-tight">Pricing that scales with you</h1>
        <p className="mt-4" style={{ color: "var(--muted-foreground)" }}>
          Start free. Upgrade when you're ready. No hidden seats or per-testimonial fees.
        </p>
        <div className="inline-flex p-1 mt-8 rounded-lg" style={{ background: "var(--surface)", border: "1px solid var(--border)" }}>
          {[
            { l: "Monthly", v: false },
            { l: "Annual · save 20%", v: true },
          ].map((o) => (
            <button
              key={o.l}
              onClick={() => setAnnual(o.v)}
              className="px-4 py-1.5 rounded-md text-sm font-medium"
              style={{ background: annual === o.v ? "var(--primary)" : "transparent", color: annual === o.v ? "white" : "var(--muted-foreground)" }}
            >
              {o.l}
            </button>
          ))}
        </div>
      </section>

      <section className="px-6 max-w-6xl mx-auto grid md:grid-cols-2 lg:grid-cols-4 gap-4">
        {plans.map((p) => (
          <div key={p.name} className="tp-card p-6 relative" style={p.featured ? { border: "1.5px solid var(--primary)" } : undefined}>
            {p.featured && (
              <div className="absolute -top-3 left-6 tp-pill" style={{ background: "var(--primary)", color: "white" }}>
                MOST POPULAR
              </div>
            )}
            <div className="font-semibold">{p.name}</div>
            <div className="mt-3 flex items-baseline gap-1">
              <span className="text-4xl font-semibold">${annual ? p.priceA : p.priceM}</span>
              <span style={{ color: "var(--subtle)" }}>/mo</span>
            </div>
            <Link to="/register" className={`tp-btn ${p.featured ? "tp-btn-primary" : "tp-btn-ghost"} w-full mt-5`}>
              Start free trial
            </Link>
          </div>
        ))}
      </section>

      <section className="px-6 py-20 max-w-6xl mx-auto">
        <h2 className="text-2xl font-semibold tracking-tight mb-6">Compare features</h2>
        <div className="tp-card overflow-hidden">
          <table className="w-full text-sm">
            <thead style={{ background: "var(--surface)" }}>
              <tr>
                <th className="px-5 py-4 text-left font-medium" style={{ color: "var(--muted-foreground)" }}>
                  Feature
                </th>
                {plans.map((p) => (
                  <th key={p.name} className="px-5 py-4 text-left font-medium">
                    {p.name}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.label} style={{ borderTop: "1px solid var(--border)" }}>
                  <td className="px-5 py-3.5" style={{ color: "var(--muted-foreground)" }}>
                    {r.label}
                  </td>
                  {r.v.map((c, i) => (
                    <td key={i} className="px-5 py-3.5">
                      {c === true ? (
                        <Check size={16} style={{ color: "var(--success)" }} />
                      ) : c === false ? (
                        <Minus size={16} style={{ color: "var(--subtle)" }} />
                      ) : (
                        <span>{c}</span>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section id="faq" className="px-6 pb-20 max-w-3xl mx-auto">
        <h2 className="text-2xl font-semibold tracking-tight mb-6">Frequently asked</h2>
        <div className="space-y-2">
          {faqs.map((f, i) => (
            <FaqItem key={f.q} q={f.q} a={f.a} defaultOpen={i === 0} />
          ))}
        </div>
      </section>

      <Footer />
    </div>
  );
}

function FaqItem({ q, a, defaultOpen }: { q: string; a: string; defaultOpen?: boolean }) {
  const [open, setOpen] = useState(!!defaultOpen);
  return (
    <div className="tp-card">
      <button className="w-full px-5 py-4 flex justify-between items-center text-left" onClick={() => setOpen(!open)}>
        <span className="font-medium">{q}</span>
        <ChevronDown size={16} style={{ transform: open ? "rotate(180deg)" : "none", transition: "transform 0.15s" }} />
      </button>
      {open && (
        <div className="px-5 pb-4 text-sm" style={{ color: "var(--muted-foreground)" }}>
          {a}
        </div>
      )}
    </div>
  );
}
