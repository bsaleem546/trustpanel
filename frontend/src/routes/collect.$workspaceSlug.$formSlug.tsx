import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { Stars } from "@/components/Stars";
import { Video, Twitter, Check } from "lucide-react";

export const Route = createFileRoute("/collect/$workspaceSlug/$formSlug")({
  head: () => ({ meta: [{ title: "Share your experience" }] }),
  component: CollectionForm,
});

function CollectionForm() {
  const { workspaceSlug } = Route.useParams();
  const [step, setStep] = useState(1);
  const [rating, setRating] = useState(5);
  const [tab, setTab] = useState<"write" | "video">("write");
  const [text, setText] = useState("");
  const brand = "#7c6af7";

  return (
    <div className="min-h-screen flex items-center justify-center p-6" style={{ background: "#fafaf7" }}>
      <div className="w-full max-w-md rounded-2xl p-8 shadow-xl" style={{ background: "white", color: "#0f0f14" }}>
        <div className="flex items-center gap-2 mb-1">
          <div className="w-7 h-7 rounded-md flex items-center justify-center text-white font-bold text-sm" style={{ background: brand }}>
            N
          </div>
          <span className="text-sm font-medium" style={{ color: "#555" }}>
            {workspaceSlug.charAt(0).toUpperCase() + workspaceSlug.slice(1)}
          </span>
        </div>

        {step < 4 && (
          <div className="flex gap-1 mt-4 mb-6">
            {[1, 2, 3].map((n) => (
              <div key={n} className="flex-1 h-1 rounded-full" style={{ background: step >= n ? brand : "#e5e5e5" }} />
            ))}
          </div>
        )}

        {step === 1 && (
          <>
            <h1 className="text-xl font-semibold mb-2">How would you rate your experience?</h1>
            <p className="text-sm text-gray-500 mb-6">Your feedback helps us improve.</p>
            <div className="flex justify-center my-8">
              <div onClick={() => null}>
                <div className="flex gap-2">
                  {[1, 2, 3, 4, 5].map((n) => (
                    <button key={n} onClick={() => setRating(n)} className="text-4xl" style={{ color: n <= rating ? "#fbbf24" : "#e5e5e5" }}>
                      ★
                    </button>
                  ))}
                </div>
              </div>
            </div>
            <button onClick={() => setStep(2)} className="w-full py-3 rounded-lg text-white font-medium" style={{ background: brand }}>
              Continue
            </button>
          </>
        )}

        {step === 2 && (
          <>
            <h1 className="text-xl font-semibold mb-1">Share your experience</h1>
            <p className="text-sm text-gray-500 mb-5">A line or two is plenty.</p>
            <div className="flex gap-1 p-1 rounded-lg mb-4" style={{ background: "#f4f4f0" }}>
              {(["write", "video"] as const).map((t) => (
                <button
                  key={t}
                  onClick={() => setTab(t)}
                  className="flex-1 px-3 py-2 rounded-md text-sm font-medium flex items-center justify-center gap-1.5"
                  style={{ background: tab === t ? "white" : "transparent", color: tab === t ? "#0f0f14" : "#666" }}
                >
                  {t === "video" && <Video size={12} />}
                  {t === "write" ? "Write a review" : "Record a video"}
                </button>
              ))}
            </div>
            {tab === "write" ? (
              <>
                <textarea
                  value={text}
                  onChange={(e) => setText(e.target.value)}
                  rows={6}
                  className="w-full p-3 rounded-lg text-sm border resize-none focus:outline-none"
                  style={{ borderColor: "#e5e5e5" }}
                  placeholder="What did you like most? Was there a specific moment that stood out?"
                />
                <div className="text-xs text-right mt-1 text-gray-400">{text.length} / 500</div>
              </>
            ) : (
              <div className="aspect-video rounded-lg flex flex-col items-center justify-center" style={{ background: "#0f0f14", color: "white" }}>
                <div className="w-16 h-16 rounded-full flex items-center justify-center mb-3" style={{ background: "#f87171" }}>
                  <Video size={26} />
                </div>
                <div className="text-sm">Tap to record</div>
                <div className="text-xs mt-1 opacity-60">Up to 60 seconds</div>
              </div>
            )}
            <button onClick={() => setStep(3)} className="w-full py-3 rounded-lg text-white font-medium mt-5" style={{ background: brand }}>
              Continue
            </button>
          </>
        )}

        {step === 3 && (
          <>
            <h1 className="text-xl font-semibold mb-1">Your details</h1>
            <p className="text-sm text-gray-500 mb-5">So we can credit you properly.</p>
            <div className="space-y-3">
              {[
                { l: "Full name", req: true, v: "" },
                { l: "Job title", req: false, v: "" },
                { l: "Company", req: false, v: "" },
                { l: "Email", req: true, v: "" },
              ].map((f) => (
                <div key={f.l}>
                  <label className="text-xs font-medium block mb-1.5 text-gray-600">
                    {f.l} {f.req && <span style={{ color: brand }}>*</span>}
                  </label>
                  <input className="w-full p-3 rounded-lg text-sm border focus:outline-none" style={{ borderColor: "#e5e5e5" }} />
                </div>
              ))}
            </div>
            <button onClick={() => setStep(4)} className="w-full py-3 rounded-lg text-white font-medium mt-5" style={{ background: brand }}>
              Submit testimonial
            </button>
          </>
        )}

        {step === 4 && (
          <div className="text-center py-6">
            <div
              className="w-16 h-16 mx-auto rounded-full flex items-center justify-center mb-5"
              style={{ background: `${brand}22`, color: brand }}
            >
              <Check size={32} />
            </div>
            <h1 className="text-xl font-semibold mb-2">Thank you!</h1>
            <p className="text-sm text-gray-500 mb-5">
              Your testimonial has been submitted. We may reach out for a follow-up.
            </p>
            <div className="rounded-lg p-4 mb-5" style={{ background: "#fef9e7" }}>
              <div className="text-xs uppercase tracking-wider text-gray-500 mb-1">Your reward</div>
              <div className="font-mono text-lg font-semibold">THANKYOU20</div>
              <div className="text-xs text-gray-500 mt-1">20% off your next month</div>
            </div>
            <button className="w-full py-2.5 rounded-lg text-sm font-medium flex items-center justify-center gap-2" style={{ background: "#0f0f14", color: "white" }}>
              <Twitter size={14} /> Share on Twitter
            </button>
          </div>
        )}

        <div className="text-center mt-6 text-xs text-gray-400">
          Powered by <span style={{ color: brand, fontWeight: 500 }}>TrustPanel</span>
        </div>
      </div>
    </div>
  );
}
