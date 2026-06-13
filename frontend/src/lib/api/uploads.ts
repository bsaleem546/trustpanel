import { api } from "./client";

export type VideoUploadSlot = {
  uploadUrl: string;
  objectKey: string;
  expiresAt: string;
};

export const uploadsApi = {
  requestVideoSlot: (contentType: string, fileSizeBytes: number) =>
    api<VideoUploadSlot>("/api/uploads/video", {
      method: "POST",
      body: { contentType, fileSizeBytes },
    }),

  getReadUrl: (objectKey: string) =>
    api<{ readUrl: string }>(`/api/uploads/read-url?objectKey=${encodeURIComponent(objectKey)}`),

  /** Upload a file directly to R2 using a pre-signed PUT URL. Returns the object key. */
  uploadVideo: async (file: File): Promise<string> => {
    const slot = await uploadsApi.requestVideoSlot(file.type, file.size);
    await fetch(slot.uploadUrl, {
      method: "PUT",
      body: file,
      headers: { "Content-Type": file.type },
    });
    return slot.objectKey;
  },
};
