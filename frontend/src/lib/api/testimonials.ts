import { api } from "./client";

export type TestimonialStatus = "Pending" | "Approved" | "Rejected";
export type TestimonialType = "Text" | "Video";
export type TestimonialSource = "Form" | "Api" | "Csv" | "Twitter" | "GoogleBusinessProfile" | "G2" | "Trustpilot";
export type BatchAction = "Approve" | "Reject" | "Delete";

export type Submitter = {
  name: string;
  email: string | null;
  company: string | null;
  jobTitle: string | null;
  avatarPath: string | null;
};

export type Testimonial = {
  id: string;
  workspaceId: string;
  collectionFormId: string | null;
  type: TestimonialType;
  content: string;
  videoPath: string | null;
  thumbnailPath: string | null;
  rating: number | null;
  status: TestimonialStatus;
  source: TestimonialSource;
  submitter: Submitter;
  sentimentScore: number | null;
  highlight: string | null;
  tags: string[];
  featuredAt: string | null;
  editedAt: string | null;
  createdAt: string;
  updatedAt: string;
};

export type PagedResult<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
};

export const testimonialsApi = {
  list(workspaceId: string, params?: { status?: TestimonialStatus; tag?: string; page?: number; pageSize?: number }) {
    const qs = new URLSearchParams({ workspaceId });
    if (params?.status) qs.set("status", params.status);
    if (params?.tag) qs.set("tag", params.tag);
    if (params?.page) qs.set("page", String(params.page));
    if (params?.pageSize) qs.set("pageSize", String(params.pageSize));
    return api<PagedResult<Testimonial>>(`/api/testimonials/?${qs}`);
  },

  search(workspaceId: string, q: string, limit = 25) {
    const qs = new URLSearchParams({ workspaceId, q, limit: String(limit) });
    return api<{ items: Testimonial[]; total: number }>(`/api/testimonials/search?${qs}`);
  },

  get(id: string) {
    return api<Testimonial>(`/api/testimonials/${id}`);
  },

  approve(id: string) {
    return api<Testimonial>(`/api/testimonials/${id}/approve`, { method: "POST" });
  },

  reject(id: string) {
    return api<Testimonial>(`/api/testimonials/${id}/reject`, { method: "POST" });
  },

  feature(id: string, featured: boolean) {
    return api<Testimonial>(`/api/testimonials/${id}/feature?featured=${featured}`, { method: "POST" });
  },

  updateTags(id: string, tags: string[]) {
    return api<Testimonial>(`/api/testimonials/${id}/tags`, { method: "PUT", body: { tags } });
  },

  edit(id: string, content: string, rating?: number) {
    return api<Testimonial>(`/api/testimonials/${id}`, { method: "PUT", body: { content, rating } });
  },

  remove(id: string) {
    return api(`/api/testimonials/${id}`, { method: "DELETE" });
  },

  batch(workspaceId: string, testimonialIds: string[], action: BatchAction) {
    return api<{ affected: number }>("/api/testimonials/batch", {
      method: "POST",
      body: { workspaceId, testimonialIds, action },
    });
  },
};
