import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Stars } from "@/components/Stars";
import { Video, Check, Loader, AlertCircle } from "lucide-react";
import { publicFormsApi, type PublicForm, type SubmitPayload } from "@/lib/api/publicForms";

export const Route = createFileRoute("/collect/$workspaceSlug/$formSlug")({
  head: () => ({ meta: [{ title: "Share your experience" }] }),
  component: CollectionFormPage,
});

function CollectionFormPage() {
  const { workspaceSlug, formSlug } = Route.useParams();

  const { data: form, isLoading, isError } = useQuery({
    queryKey: ["public-form", workspaceSlug, formSlug],
    queryFn: () => publicFormsApi.getForm(workspaceSlug, formSlug),
    staleTime: 5 * 60_000,
    retry: 1,
  });

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center" style={{ background: "#fafaf7" }}>
        <Loader size={24} className="animate-spin text-gray-400" />
      </div>
    );
  }

  if (isError || !form) {
    return (
      <div className="min-h-screen flex items-center justify-center p-6" style={{ background: "#fafaf7" }}>
        <div className="text-center max-w-sm">
          <AlertCircle size={32} className="mx-auto mb-3 text-red-400" />
          <div className="font-semibold mb-1">Form not found</div>
          <p className="text-sm text-gray-500">This form may have been deactivated or the link is incorrect.</p>
        </div>
      </div>
    );
  }

  return <CollectionForm form={form} workspaceSlug={workspaceSlug} formSlug={formSlug} />;
}

type Props = { form: PublicForm; workspaceSlug: string; formSlug: string };

function CollectionForm({ form, workspaceSlug, formSlug }: Props) {
  const q = form.questions;
  const brand = form.primaryColor || "#7c6af7";

  const [step, setStep] = useState<"rating" | "content" | "details" | "done">(
    q.collectRating ? "rating" : "content"
  );
  const [rating, setRating] = useState(5);
  const [tab, setTab] = useState<"write" | "video">(
    form.allowedSubmissionType === "Video" ? "video" : "write"
  );
  const [content, setContent] = useState("");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [company, setCompany] = useState("");
  const [jobTitle, setJobTitle] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const steps = [
    ...(q.collectRating ? ["rating"] : []),
    "content",
    ...((q.collectName || q.collectEmail || q.collectCompany || q.collectJobTitle) ? ["details"] : []),
  ] as const;
  const stepIndex = steps.indexOf(step as never);

  async function submit() {
    setSubmitting(true);
    setSubmitError(null);
    try {
      const payload: SubmitPayload = {
        content,
        rating: q.collectRating ? rating : null,
        name: q.collectName ? name.trim() : "",
        email: q.collectEmail ? email.trim() || null : null,
        company: q.collectCompany ? company.trim() || null : null,
        jobTitle: q.collectJobTitle ? jobTitle.trim() || null : null,
      };
      await publicFormsApi.submit(workspaceSlug, formSlug, payload);
      setStep("done");
    } catch (e: unknown) {
      setSubmitError((e as Error).message ?? "Submission failed. Please try again.");
    } finally {
      setSubmitting(false);
    }
  }

  const canSubmitContent = tab === "write" ? content.trim().length > 10 : true;
  const canSubmitDetails = (!q.collectName || name.trim()) && (!q.requireEmail || email.trim());

  const hasDetails = q.collectName || q.collectEmail || q.collectCompany || q.collectJobTitle;

  function nextFromContent() {
    if (hasDetails) setStep("details");
    else submit();
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-6" style={{ background: "#fafaf7" }}>
      <div className="w-full max-w-md rounded-2xl p-8 shadow-xl" style={{ background: "white", color: "#0f0f14" }}>
        {/* Workspace header */}
        <div className="flex items-center gap-2 mb-1">
          {form.logoPath ? (
            <img src={form.logoPath} alt={form.workspaceName} className="w-7 h-7 rounded-md object-cover" />
          ) : (
            <div className="w-7 h-7 rounded-md flex items-center justify-center text-white font-bold text-sm"
              style={{ background: brand }}>
              {form.workspaceName.charAt(0).toUpperCase()}
            </div>
          )}
          <span className="text-sm font-medium" style={{ color: "#555" }}>{form.workspaceName}</span>
        </div>

        {/* Progress bar */}
        {step !== "done" && steps.length > 1 && (
          <div className="flex gap-1 mt-4 mb-6">
            {steps.map((s, i) => (
              <div key={s} className="flex-1 h-1 rounded-full"
                style={{ background: i <= stepIndex ? brand : "#e5e5e5" }} />
            ))}
          </div>
        )}

        {/* Step: Rating */}
        {step === "rating" && (
          <>
            <h1 className="text-xl font-semibold mb-2">{q.welcomeTitle || "How would you rate your experience?"}</h1>
            <p className="text-sm text-gray-500 mb-6">{q.welcomeMessage || "Your feedback helps us improve."}</p>
            <div className="flex justify-center my-8">
              <div className="flex gap-2">
                {[1, 2, 3, 4, 5].map((n) => (
                  <button key={n} onClick={() => setRating(n)} className="text-4xl"
                    style={{ color: n <= rating ? "#fbbf24" : "#e5e5e5" }}>
                    ★
                  </button>
                ))}
              </div>
            </div>
            <button onClick={() => setStep("content")} className="w-full py-3 rounded-lg text-white font-medium"
              style={{ background: brand }}>
              Continue
            </button>
          </>
        )}

        {/* Step: Content */}
        {step === "content" && (
          <>
            <h1 className="text-xl font-semibold mb-1">Share your experience</h1>
            <p className="text-sm text-gray-500 mb-5">{q.prompt || "A line or two is plenty."}</p>

            {form.allowedSubmissionType === "Both" && (
              <div className="flex gap-1 p-1 rounded-lg mb-4" style={{ background: "#f4f4f0" }}>
                {(["write", "video"] as const).map((t) => (
                  <button key={t} onClick={() => setTab(t)}
                    className="flex-1 px-3 py-2 rounded-md text-sm font-medium flex items-center justify-center gap-1.5"
                    style={{ background: tab === t ? "white" : "transparent", color: tab === t ? "#0f0f14" : "#666" }}>
                    {t === "video" && <Video size={12} />}
                    {t === "write" ? "Write a review" : "Record a video"}
                  </button>
                ))}
              </div>
            )}

            {tab === "write" ? (
              <>
                <textarea
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  rows={6}
                  maxLength={2000}
                  className="w-full p-3 rounded-lg text-sm border resize-none focus:outline-none"
                  style={{ borderColor: "#e5e5e5" }}
                  placeholder="What did you like most? Was there a specific moment that stood out?"
                />
                <div className="text-xs text-right mt-1 text-gray-400">{content.length} / 2000</div>
              </>
            ) : (
              <div className="aspect-video rounded-lg flex flex-col items-center justify-center"
                style={{ background: "#0f0f14", color: "white" }}>
                <div className="w-16 h-16 rounded-full flex items-center justify-center mb-3"
                  style={{ background: "#f87171" }}>
                  <Video size={26} />
                </div>
                <div className="text-sm">Video recording</div>
                <div className="text-xs mt-1 opacity-60">Coming soon</div>
              </div>
            )}

            <button onClick={nextFromContent} disabled={!canSubmitContent || submitting}
              className="w-full py-3 rounded-lg text-white font-medium mt-5 disabled:opacity-50"
              style={{ background: brand }}>
              {hasDetails ? "Continue" : (submitting ? "Submitting…" : "Submit testimonial")}
            </button>
          </>
        )}

        {/* Step: Details */}
        {step === "details" && (
          <>
            <h1 className="text-xl font-semibold mb-1">Your details</h1>
            <p className="text-sm text-gray-500 mb-5">So we can credit you properly.</p>
            <div className="space-y-3">
              {q.collectName && (
                <FormField label="Full name" required value={name} onChange={setName} brand={brand} />
              )}
              {q.collectJobTitle && (
                <FormField label="Job title" value={jobTitle} onChange={setJobTitle} brand={brand} />
              )}
              {q.collectCompany && (
                <FormField label="Company" value={company} onChange={setCompany} brand={brand} />
              )}
              {q.collectEmail && (
                <FormField label="Email" type="email" required={q.requireEmail} value={email} onChange={setEmail} brand={brand} />
              )}
            </div>
            {submitError && (
              <p className="text-xs mt-3 text-red-500">{submitError}</p>
            )}
            <button onClick={submit} disabled={!canSubmitDetails || submitting}
              className="w-full py-3 rounded-lg text-white font-medium mt-5 disabled:opacity-50"
              style={{ background: brand }}>
              {submitting ? "Submitting…" : "Submit testimonial"}
            </button>
          </>
        )}

        {/* Step: Done */}
        {step === "done" && (
          <div className="text-center py-6">
            <div className="w-16 h-16 mx-auto rounded-full flex items-center justify-center mb-5"
              style={{ background: `${brand}22`, color: brand }}>
              <Check size={32} />
            </div>
            <h1 className="text-xl font-semibold mb-2">Thank you!</h1>
            <p className="text-sm text-gray-500 mb-5">
              Your testimonial has been submitted. We may reach out for a follow-up.
            </p>
          </div>
        )}

        {form.showTrustPanelBranding && (
          <div className="text-center mt-6 text-xs text-gray-400">
            Powered by <span style={{ color: brand, fontWeight: 500 }}>TrustPanel</span>
          </div>
        )}
      </div>
    </div>
  );
}

function FormField({
  label, required, value, onChange, brand, type = "text",
}: {
  label: string; required?: boolean; value: string; onChange: (v: string) => void;
  brand: string; type?: string;
}) {
  return (
    <div>
      <label className="text-xs font-medium block mb-1.5 text-gray-600">
        {label} {required && <span style={{ color: brand }}>*</span>}
      </label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full p-3 rounded-lg text-sm border focus:outline-none"
        style={{ borderColor: "#e5e5e5" }}
      />
    </div>
  );
}
