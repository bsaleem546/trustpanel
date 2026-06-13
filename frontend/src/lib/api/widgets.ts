import { api } from "./client";
import type { TestimonialSource } from "./testimonials";

export type WidgetType = "Carousel" | "MasonryGrid" | "Badge" | "Popup" | "Slider" | "SingleCard";

export type WidgetSettings = {
  cardStyle: string;
  primaryColor: string;
  backgroundColor: string;
  textColor: string;
  fontSize: string;
  animation: string;
  darkMode: boolean;
  showRating: boolean;
  showAvatar: boolean;
  showDate: boolean;
  showSource: boolean;
};

export type Widget = {
  id: string;
  workspaceId: string;
  type: WidgetType;
  name: string;
  filterTags: string[];
  minimumRating: number | null;
  featuredOnly: boolean;
  selectedTestimonialIds: string[];
  sourceFilter: TestimonialSource | null;
  settings: WidgetSettings;
  customCss: string | null;
  createdAt: string;
  updatedAt: string;
};

export type WidgetInput = {
  workspaceId: string;
  type: WidgetType;
  name: string;
  filterTags?: string[];
  minimumRating?: number;
  featuredOnly?: boolean;
  selectedTestimonialIds?: string[];
  sourceFilter?: TestimonialSource;
  settings?: Partial<WidgetSettings>;
  customCss?: string;
};

export const widgetsApi = {
  list(workspaceId: string) {
    return api<{ items: Widget[]; total: number }>(`/api/widgets/?workspaceId=${workspaceId}`);
  },

  get(widgetId: string) {
    return api<Widget>(`/api/widgets/${widgetId}`);
  },

  create(input: WidgetInput) {
    return api<Widget>("/api/widgets/", { method: "POST", body: input });
  },

  update(widgetId: string, input: WidgetInput) {
    return api<Widget>(`/api/widgets/${widgetId}`, { method: "PUT", body: input });
  },

  remove(widgetId: string) {
    return api(`/api/widgets/${widgetId}`, { method: "DELETE" });
  },
};
