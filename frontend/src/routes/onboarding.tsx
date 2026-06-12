import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { Check, Upload, Copy } from "lucide-react";
import { Logo } from "@/components/Logo";
import { Stars } from "@/components/Stars";

export const Route = createFileRoute("/onboarding")({
  head: () => ({ meta: [{ title: "Welcome — TrustPanel" }] }),
  component: Onboarding,
});

function Onboarding() {
  const [step, setStep] = useState(1);
  const [slug, setSlug] = useState("northwind");
  const [color, setColor] = useState("#7c6af7");
  const [formName, setFormName] = useState("Homepage testimonial");

  return (
    <div className="min-h-screen flex flex-col">
      <header className="px-6 py-4 flex items-center justify-between border-b" style={{ borderColor: "var(--border)" }}>
        <Logo />
        <Link to="/dashboard" className="text-sm" style={{ color: "var(--muted-foreground)" }}>
          Skip for now
        </Link>
      </header>
      <div className="px-6 py-10 max-w-3xl w-full mx-auto flex-1">
        <div className="flex items-center gap-2 mb-8">
          {[1, 2, 3, 4].map((n) => (
            <div key={n} className="flex-1 flex items-center gap-2">
              <div
                className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-medium"
                style={{
                  background: step >= n ? "var(--primary)" : "var(--surface)",
                  color: step >= n ? "white" : "var(--subtle)",
                  border: "1px solid var(--border)",
                }}
              >
                {step > n ? <Check size={14} /> : n}
              </div>
              {n < 4 && <div className="flex-1 h-px" style={{ background: step > n ? "var(--primary)" : "var(--border)" }} />}
            </div>
          ))}
        </div>

        <div className="tp-card p-8">
          {step === 1 && (
            <Step title="What should we call your workspace?" subtitle="You can change this anytime.">
              <Field label="Workspace name">
                <input className="tp-input" defaultValue="Northwind Agency" />
              </Field>
              <Field label="Workspace URL">
                <div className="flex items-center gap-2">
                  <span className="text-sm" style={{ color: "var(--subtle)" }}>
                    trustpanel.com/c/
                  </span>
                  <input className="tp-input" value={slug} onChange={(e) => setSlug(e.target.value)} />
                </div>
              </Field>
              <Field label="Industry (optional)">
                <select className="tp-input">
                  <option>Agency / Studio</option>
                  <option>SaaS</option>
                  <option>E-commerce</option>
                  <option>Education</option>
                </select>
              </Field>
            </Step>
          )}
          {step === 2 && (
            <Step title="Brand your workspace" subtitle="Submitters will see your brand on the collection page.">
              <Field label="Logo">
                <div
                  className="border-2 border-dashed rounded-lg p-8 text-center text-sm"
                  style={{ borderColor: "var(--border-hover)", color: "var(--subtle)" }}
                >
                  <Upload size={20} className="mx-auto mb-2" />
                  Drag and drop your logo, or click to browse
                </div>
              </Field>
              <Field label="Primary color">
                <div className="flex items-center gap-3">
                  <input type="color" value={color} onChange={(e) => setColor(e.target.value)} className="w-10 h-10 rounded cursor-pointer border-0" />
                  <input className="tp-input font-mono" value={color} onChange={(e) => setColor(e.target.value)} />
                </div>
              </Field>
              <div className="tp-card p-5 mt-4" style={{ background: "var(--surface)" }}>
                <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--subtle)" }}>
                  Preview
                </div>
                <div className="text-lg font-medium mb-1">Share your experience with Northwind Agency</div>
                <Stars value={4} size={20} />
                <button className="mt-4 px-4 py-2 rounded-lg text-white text-sm font-medium" style={{ background: color }}>
                  Continue
                </button>
              </div>
            </Step>
          )}
          {step === 3 && (
            <Step title="Create your first form" subtitle="You can build more forms later.">
              <Field label="Form name">
                <input className="tp-input" value={formName} onChange={(e) => setFormName(e.target.value)} />
              </Field>
              <Field label="Allowed submission types">
                <div className="grid grid-cols-2 gap-2">
                  {["Text", "Video", "Both"].map((t) => (
                    <button key={t} className="tp-btn tp-btn-ghost justify-start">
                      <Check size={14} style={{ color: "var(--success)" }} /> {t}
                    </button>
                  ))}
                </div>
              </Field>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" defaultChecked /> Require a star rating
              </label>
            </Step>
          )}
          {step === 4 && (
            <Step title="Embed your form" subtitle="Paste this snippet anywhere on your site.">
              <div className="font-mono text-sm tp-card p-4 relative" style={{ background: "var(--surface)" }}>
                <button className="absolute top-3 right-3 tp-btn tp-btn-ghost" style={{ padding: "4px 8px" }}>
                  <Copy size={12} /> Copy
                </button>
                <pre className="whitespace-pre-wrap break-all" style={{ color: "var(--primary-light)" }}>
{`<script src="https://cdn.trustpanel.com/embed.js"
  data-workspace="${slug}"
  data-form="${formName.toLowerCase().replace(/\s+/g, "-")}"
></script>`}
                </pre>
              </div>
            </Step>
          )}

          <div className="mt-8 flex justify-between">
            <button
              className="tp-btn tp-btn-ghost"
              onClick={() => setStep(Math.max(1, step - 1))}
              style={{ visibility: step === 1 ? "hidden" : "visible" }}
            >
              Back
            </button>
            {step < 4 ? (
              <button className="tp-btn tp-btn-primary" onClick={() => setStep(step + 1)}>
                Continue
              </button>
            ) : (
              <Link to="/dashboard" className="tp-btn tp-btn-primary">
                Continue to dashboard
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function Step({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <div>
      <h2 className="text-xl font-semibold tracking-tight">{title}</h2>
      <p className="text-sm mt-1 mb-6" style={{ color: "var(--muted-foreground)" }}>
        {subtitle}
      </p>
      <div className="space-y-4">{children}</div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="text-xs font-medium mb-1.5 block" style={{ color: "var(--muted-foreground)" }}>
        {label}
      </label>
      {children}
    </div>
  );
}
