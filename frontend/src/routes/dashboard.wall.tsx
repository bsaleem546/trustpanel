import { createFileRoute } from "@tanstack/react-router";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars } from "@/components/Stars";
import { testimonials } from "@/lib/mock-data";

export const Route = createFileRoute("/dashboard/wall")({
  head: () => ({ meta: [{ title: "Wall of Love — TrustPanel" }] }),
  component: Wall,
});

function Wall() {
  const approved = testimonials.filter((t) => t.status !== "rejected" && t.status !== "pending");
  return (
    <DashboardLayout title="Wall of Love preview">
      <div className="tp-card p-8" style={{ background: "var(--surface)" }}>
        <div className="columns-1 md:columns-2 lg:columns-3 gap-4 space-y-4">
          {approved.map((t) => (
            <div key={t.id} className="tp-card p-5 break-inside-avoid">
              <Stars value={t.rating} />
              <p className="text-sm mt-3 leading-relaxed">"{t.text}"</p>
              <div className="flex items-center gap-2.5 mt-4">
                <Avatar name={t.name} color={t.avatarColor} size={32} />
                <div>
                  <div className="text-sm font-medium">{t.name}</div>
                  <div className="text-xs" style={{ color: "var(--subtle)" }}>
                    {t.company}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </DashboardLayout>
  );
}
