import { createFileRoute, useNavigate, useSearch } from "@tanstack/react-router";
import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Stars } from "@/components/Stars";
import { formsApi, type SubmissionType } from "@/lib/api/forms";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/forms/create")({
  head: () => ({ meta: [{ title: "Build form — TrustPanel" }] }),
  validateSearch: (s: Record<string, unknown>) => ({ edit: s.edit as string | undefined }),
  component: FormBuilder,
});

function Toggle({
  label,
  checked,
  onChange,
  locked,
}: {
  label: string;
  checked: boolean;
  onChange?: (v: boolean) => void;
  locked?: boolean;
}) {
  return (
    <label className="flex items-center justify-between py-2.5">
      <span className="text-sm">
        {label}
        {locked && (
          <span className="ml-2 text-xs" style={{ color: "var(--subtle)" }}>
            required
          </span>
        )}
      </span>
      <button
        type="button"
        onClick={() => !locked && onChange?.(!checked)}
        disabled={locked}
        className="w-9 h-5 rounded-full transition-colors relative"
        style={{ background: checked || locked ? "var(--primary)" : "var(--border)" }}
      >
        <span
          className="absolute top-0.5 w-4 h-4 rounded-full bg-white transition-all"
          style={{ left: checked || locked ? 18 : 2 }}
        />
      </button>
    </label>
  );
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="tp-card p-5">
      <div className="text-xs uppercase tracking-wider mb-3" style={{ color: "var(--primary-light)" }}>
        {title}
      </div>
      <div className="space-y-1">{children}</div>
    </div>
  );
}

function FormBuilder() {
  useRequireAuth();
  const { data: me } = useMe();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { edit: editId } = useSearch({ from: "/dashboard/forms/create" });
  const isEditing = !!editId;

  const [previewStep, setPreviewStep] = useState<"rating" | "submission" | "thanks">("rating");
  const [name, setName] = useState("Homepage testimonial");
  const [submissionType, setSubmissionType] = useState<SubmissionType>("Both");

  // Pre-fill when editing.
  useQuery({
    queryKey: ["form", editId],
    queryFn: () => formsApi.get(editId!),
    enabled: !!editId,
    staleTime: Infinity,
    select: (form) => {
      setName(form.name);
      setSubmissionType(form.allowedSubmissionType);
      return form;
    },
  });

  const save = useMutation({
    mutationFn: () =>
      isEditing
        ? formsApi.update(editId!, { name, allowedSubmissionType: submissionType, isActive: true })
        : formsApi.create({ name, allowedSubmissionType: submissionType, isActive: true }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["forms"] });
      navigate({ to: "/dashboard/forms" });
    },
  });

  const workspaceName = me?.email?.split("@")[1]?.split(".")[0] ?? "your workspace";

  return (
    <DashboardLayout
      title={isEditing ? "Edit form" : "Form builder"}
      action={
        <>
          <button className="tp-btn tp-btn-ghost" onClick={() => navigate({ to: "/dashboard/forms" })}>
            Discard
          </button>
          <button
            className="tp-btn tp-btn-primary"
            onClick={() => save.mutate()}
            disabled={save.isPending || !name.trim()}
          >
            {save.isPending ? "Saving…" : isEditing ? "Save changes" : "Publish form"}
          </button>
        </>
      }
    >
      {save.isError && (
        <div className="tp-card p-3 mb-4 text-sm" style={{ color: "var(--danger)", borderColor: "var(--danger)" }}>
          Failed to save form. Please try again.
        </div>
      )}

      <div className="grid lg:grid-cols-[1fr_400px] gap-6">
        <div className="space-y-4">
          <div className="tp-card p-5">
            <label className="text-xs font-medium block mb-1.5" style={{ color: "var(--muted-foreground)" }}>
              Form name
            </label>
            <input
              className="tp-input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Homepage testimonial"
            />
          </div>

          <Section title="Questions to show">
            <Toggle label="Star rating" checked locked />
            <Toggle label="Name" checked locked />
            <Toggle label="Job title" checked onChange={() => {}} />
            <Toggle label="Company name" checked onChange={() => {}} />
            <Toggle label="Profile photo" checked={false} onChange={() => {}} />
          </Section>

          <Section title="Submission type">
            <div className="grid grid-cols-3 gap-2">
              {(["Text", "Video", "Both"] as const).map((t) => (
                <button
                  key={t}
                  onClick={() => setSubmissionType(t)}
                  className="px-3 py-2.5 rounded-lg text-sm font-medium capitalize"
                  style={{
                    background: submissionType === t ? "var(--primary-soft)" : "var(--surface)",
                    border: `1px solid ${submissionType === t ? "var(--primary)" : "var(--border)"}`,
                    color: submissionType === t ? "var(--primary-light)" : "var(--muted-foreground)",
                  }}
                >
                  {t === "Both" ? "Both" : t === "Text" ? "Text only" : "Video only"}
                </button>
              ))}
            </div>
          </Section>

          {submissionType !== "Text" && (
            <Section title="Video settings">
              <label className="text-xs font-medium block mb-1.5" style={{ color: "var(--muted-foreground)" }}>
                Max duration
              </label>
              <div className="grid grid-cols-4 gap-2">
                {["30s", "60s", "90s", "3 min"].map((d, i) => (
                  <button
                    key={d}
                    className="px-3 py-2 rounded-lg text-sm"
                    style={{
                      background: i === 1 ? "var(--primary-soft)" : "var(--surface)",
                      border: `1px solid ${i === 1 ? "var(--primary)" : "var(--border)"}`,
                      color: i === 1 ? "var(--primary-light)" : "var(--muted-foreground)",
                    }}
                  >
                    {d}
                  </button>
                ))}
              </div>
            </Section>
          )}

          <Section title="After submission">
            <label className="text-xs font-medium block mb-1.5" style={{ color: "var(--muted-foreground)" }}>
              Thank-you message
            </label>
            <textarea
              className="tp-input"
              rows={3}
              defaultValue="Thank you! Your testimonial is now in our queue for review."
            />
            <label className="text-xs font-medium block mt-3 mb-1.5" style={{ color: "var(--muted-foreground)" }}>
              Redirect URL (optional)
            </label>
            <input className="tp-input" placeholder="https://yoursite.com/thanks" />
          </Section>

          <Section title="Notifications">
            <label className="text-xs font-medium block mb-1.5" style={{ color: "var(--muted-foreground)" }}>
              Notify on submission
            </label>
            <input className="tp-input" defaultValue={me?.email ?? ""} />
            <Toggle label="Auto-approve if rating ≥ 4 and sentiment positive" checked onChange={() => {}} />
          </Section>
        </div>

        {/* Preview */}
        <div className="sticky top-20 h-fit">
          <div className="tp-card p-4">
            <div className="flex gap-1 mb-4">
              {(["rating", "submission", "thanks"] as const).map((s) => (
                <button
                  key={s}
                  onClick={() => setPreviewStep(s)}
                  className="flex-1 px-2 py-1.5 rounded-md text-xs font-medium capitalize"
                  style={{
                    background: previewStep === s ? "var(--primary-soft)" : "transparent",
                    color: previewStep === s ? "var(--primary-light)" : "var(--muted-foreground)",
                  }}
                >
                  {s}
                </button>
              ))}
            </div>
            <div
              className="rounded-2xl p-6 mx-auto"
              style={{ width: 320, background: "white", color: "#0f0f14", minHeight: 480 }}
            >
              <div className="text-xs uppercase tracking-wider mb-2" style={{ color: "#7c6af7" }}>
                {workspaceName}
              </div>
              {previewStep === "rating" && (
                <>
                  <div className="text-lg font-medium mb-6">How would you rate your experience?</div>
                  <Stars value={0} size={28} />
                </>
              )}
              {previewStep === "submission" && (
                <>
                  <div className="text-lg font-medium mb-3">Share your experience</div>
                  <div className="flex gap-1 mb-4 text-xs">
                    <span className="px-3 py-1.5 rounded-md text-white" style={{ background: "#7c6af7" }}>Write</span>
                    {submissionType !== "Text" && <span className="px-3 py-1.5 rounded-md bg-gray-100">Record video</span>}
                  </div>
                  <div className="border rounded-lg p-3 text-sm text-gray-400 min-h-[120px]">
                    Tell us what you liked…
                  </div>
                </>
              )}
              {previewStep === "thanks" && (
                <div className="text-center pt-12">
                  <div
                    className="w-16 h-16 mx-auto rounded-full flex items-center justify-center text-3xl"
                    style={{ background: "#34d39922", color: "#34d399" }}
                  >
                    ✓
                  </div>
                  <div className="mt-4 text-lg font-medium">Thanks!</div>
                  <div className="text-sm text-gray-500 mt-1">Your testimonial is on its way.</div>
                </div>
              )}
              <button
                className="mt-6 w-full py-2.5 rounded-lg text-white text-sm font-medium"
                style={{ background: "#7c6af7" }}
              >
                Continue
              </button>
            </div>
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
