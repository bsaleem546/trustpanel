import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Avatar, Stars } from "@/components/Stars";
import { testimonialsApi } from "@/lib/api/testimonials";
import { useMe, useRequireAuth } from "@/lib/auth";

export const Route = createFileRoute("/dashboard/wall")({
  head: () => ({ meta: [{ title: "Wall of Love — TrustPanel" }] }),
  component: Wall,
});

function Wall() {
  useRequireAuth();
  const { data: me } = useMe();

  const { data, isLoading } = useQuery({
    queryKey: ["testimonials", me?.workspaceId, "Approved", "wall"],
    queryFn: () =>
      testimonialsApi.list(me!.workspaceId!, { status: "Approved", pageSize: 100 }),
    enabled: !!me?.workspaceId,
    staleTime: 60_000,
  });

  const items = data?.items ?? [];

  return (
    <DashboardLayout title="Wall of Love preview">
      <div className="tp-card p-8" style={{ background: "var(--surface)" }}>
        {isLoading && (
          <div className="text-sm text-center py-8" style={{ color: "var(--subtle)" }}>
            Loading testimonials…
          </div>
        )}
        {!isLoading && items.length === 0 && (
          <div className="text-sm text-center py-8" style={{ color: "var(--subtle)" }}>
            No approved testimonials yet. Approve some from the Testimonials inbox.
          </div>
        )}
        <div className="columns-1 md:columns-2 lg:columns-3 gap-4 space-y-4">
          {items.map((t) => (
            <div key={t.id} className="tp-card p-5 break-inside-avoid">
              {t.rating && <Stars value={t.rating} />}
              <p className="text-sm mt-3 leading-relaxed">"{t.content}"</p>
              <div className="flex items-center gap-2.5 mt-4">
                <Avatar name={t.submitter.name} color="#a594f9" size={32} />
                <div>
                  <div className="text-sm font-medium">{t.submitter.name}</div>
                  {t.submitter.company && (
                    <div className="text-xs" style={{ color: "var(--subtle)" }}>
                      {t.submitter.company}
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </DashboardLayout>
  );
}
