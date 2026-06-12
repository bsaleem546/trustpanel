// Thin fetch wrapper for the TrustPanel .NET API.
//
// Every backend response uses the same envelope (ApiResponse<T> on the server):
//   { code, status, data, message, error, errors }
// `api()` unwraps it: resolves with `data` on success, throws ApiError otherwise.
//
// Auth model: short-lived access token kept in localStorage + memory, sent as a
// Bearer header. The refresh token lives in an httpOnly cookie scoped to
// /api/auth, so a 401 triggers one silent POST /api/auth/refresh and a retry.
// In dev, Vite proxies /api to the backend so the cookie stays same-origin.

export type ApiEnvelope<T> = {
  code: number;
  status: boolean;
  data: T;
  message: string;
  error: string;
  errors: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly code: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(code: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = "ApiError";
    this.code = code;
    this.fieldErrors = fieldErrors;
  }
}

const TOKEN_KEY = "tp_access_token";
const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? "";

let accessToken: string | null = null;

export function getAccessToken(): string | null {
  if (accessToken) return accessToken;
  if (typeof window === "undefined") return null;
  accessToken = window.localStorage.getItem(TOKEN_KEY);
  return accessToken;
}

export function setAccessToken(token: string | null) {
  accessToken = token;
  if (typeof window === "undefined") return;
  if (token) {
    window.localStorage.setItem(TOKEN_KEY, token);
  } else {
    window.localStorage.removeItem(TOKEN_KEY);
  }
}

export function isAuthenticated(): boolean {
  return getAccessToken() !== null;
}

export type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  /** Attach the Bearer token and refresh-retry on 401. Default true. */
  auth?: boolean;
};

async function send<T>(path: string, options: RequestOptions): Promise<ApiEnvelope<T>> {
  const headers: Record<string, string> = {};
  if (options.body !== undefined) headers["Content-Type"] = "application/json";

  const token = options.auth === false ? null : getAccessToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    credentials: "include",
  });

  try {
    return (await response.json()) as ApiEnvelope<T>;
  } catch {
    throw new ApiError(response.status, "The server returned an unexpected response.");
  }
}

// Single-flight: concurrent 401s share one refresh request.
let refreshInFlight: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  refreshInFlight ??= send<{ accessToken: string }>("/api/auth/refresh", {
    method: "POST",
    auth: false,
  })
    .then((envelope) => {
      if (envelope.status && envelope.data?.accessToken) {
        setAccessToken(envelope.data.accessToken);
        return true;
      }
      return false;
    })
    .catch(() => false)
    .finally(() => {
      refreshInFlight = null;
    });
  return refreshInFlight;
}

export async function api<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  let envelope = await send<T>(path, options);

  const refreshable =
    envelope.code === 401 && options.auth !== false && !path.startsWith("/api/auth/");
  if (refreshable) {
    if (await tryRefresh()) {
      envelope = await send<T>(path, options);
    } else {
      setAccessToken(null);
    }
  }

  if (!envelope.status) {
    throw new ApiError(envelope.code, envelope.message || envelope.error, envelope.errors);
  }
  return envelope.data;
}
