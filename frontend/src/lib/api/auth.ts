import { api, setAccessToken } from "./client";

export type AuthUser = {
  id: string;
  email: string;
  role: string;
  onboardingCompleted: boolean;
};

export type AuthPayload = {
  accessToken: string;
  accessTokenExpiresAt: string;
  sessionId: string;
  workspaceId: string | null;
  user: AuthUser;
};

export type Me = AuthUser & { workspaceId: string | null };

export type SessionInfo = {
  id: string;
  userAgent: string;
  ipAddress: string;
  createdAt: string;
  isCurrent?: boolean;
};

export type OnboardingState = {
  workspaceName: string | null;
  logoPath: string | null;
  firstFormTemplate: string | null;
  embedSnippetViewed: boolean;
  completed: boolean;
};

export type OnboardingInput = Partial<{
  workspaceName: string;
  logoPath: string;
  firstFormTemplate: string;
  embedSnippetViewed: boolean;
  completed: boolean;
}>;

export const authApi = {
  register(input: { email: string; password: string; workspaceName?: string }) {
    return api<{ userId: string; workspaceId: string }>("/api/auth/register", {
      method: "POST",
      body: input,
      auth: false,
    });
  },

  confirmEmail(input: { userId: string; token: string }) {
    return api("/api/auth/confirm-email", { method: "POST", body: input, auth: false });
  },

  async login(input: { email: string; password: string }) {
    const payload = await api<AuthPayload>("/api/auth/login", {
      method: "POST",
      body: input,
      auth: false,
    });
    setAccessToken(payload.accessToken);
    return payload;
  },

  /** Exchanges the httpOnly refresh cookie for a new access token (used after Google sign-in). */
  async refreshSession() {
    const payload = await api<AuthPayload>("/api/auth/refresh", { method: "POST", auth: false });
    setAccessToken(payload.accessToken);
    return payload;
  },

  async logout() {
    try {
      await api("/api/auth/logout", { method: "POST" });
    } finally {
      setAccessToken(null);
    }
  },

  me() {
    return api<Me>("/api/auth/me");
  },

  sessions() {
    return api<{ items: SessionInfo[]; total: number }>("/api/auth/sessions");
  },

  revokeSession(sessionId: string) {
    return api(`/api/auth/sessions/${sessionId}`, { method: "DELETE" });
  },

  forgotPassword(email: string) {
    return api("/api/auth/forgot-password", { method: "POST", body: { email }, auth: false });
  },

  resetPassword(input: { email: string; token: string; newPassword: string }) {
    return api("/api/auth/reset-password", { method: "POST", body: input, auth: false });
  },

  updateOnboarding(input: OnboardingInput) {
    return api<OnboardingState>("/api/auth/onboarding", { method: "PUT", body: input });
  },
};

/** URL that starts the Google OAuth flow (full-page navigation, not fetch). */
export const googleSignInUrl = "/api/auth/google";
