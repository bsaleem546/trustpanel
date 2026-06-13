// No auth required — these are anonymous public endpoints

export type SubmissionType = "Text" | "Video" | "Both";

export type QuestionConfig = {
  welcomeTitle: string;
  welcomeMessage: string;
  prompt: string;
  collectName: boolean;
  collectEmail: boolean;
  collectCompany: boolean;
  collectJobTitle: boolean;
  collectAvatar: boolean;
  collectRating: boolean;
  requireEmail: boolean;
};

export type PublicForm = {
  formId: string;
  workspaceId: string;
  formSlug: string;
  name: string;
  allowedSubmissionType: SubmissionType;
  questions: QuestionConfig;
  workspaceName: string;
  logoPath: string | null;
  primaryColor: string;
  secondaryColor: string;
  fontFamily: string;
  showTrustPanelBranding: boolean;
};

export type SubmitPayload = {
  content: string;
  rating: number | null;
  name: string;
  email: string | null;
  company: string | null;
  jobTitle: string | null;
  turnstileToken?: string | null;
};

async function publicFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, { ...init, headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) } });
  const body = await res.json();
  if (!res.ok) throw new Error(body?.error ?? body?.message ?? "Request failed");
  return body.data as T;
}

export const publicFormsApi = {
  getForm: (workspaceSlug: string, formSlug: string): Promise<PublicForm> =>
    publicFetch(`/api/public/forms/${encodeURIComponent(workspaceSlug)}/${encodeURIComponent(formSlug)}`),

  submit: (workspaceSlug: string, formSlug: string, payload: SubmitPayload): Promise<unknown> =>
    publicFetch(
      `/api/public/forms/${encodeURIComponent(workspaceSlug)}/${encodeURIComponent(formSlug)}/submissions`,
      { method: "POST", body: JSON.stringify(payload) },
    ),
};
