import { api } from "./client";

export type DailyCount = { date: string; count: number };
export type RatingBucket = { rating: number; count: number };
export type StringBucket = { key: string; count: number };

export type AnalyticsDashboard = {
  submissionsOverTime: DailyCount[];
  impressionsOverTime: DailyCount[];
  ratingDistribution: RatingBucket[];
  topCountries: StringBucket[];
  topDevices: StringBucket[];
  totalApproved: number;
  totalPending: number;
};

export const analyticsApi = {
  dashboard(workspaceId: string, daysBack = 30) {
    return api<AnalyticsDashboard>(
      `/api/analytics/dashboard?workspaceId=${workspaceId}&daysBack=${daysBack}`
    );
  },

  exportCsvUrl(workspaceId: string, daysBack = 30) {
    return `/api/analytics/export/csv?workspaceId=${workspaceId}&daysBack=${daysBack}`;
  },
};
