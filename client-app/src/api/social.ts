import apiClient from './client';
import type {
  InstagramConnectionStatus,
  OpenAiTestResult,
  SocialConfig,
  SocialDbDiagnostics,
  SocialPostsWeek,
  SocialPost,
  SocialSchemaBootstrapResult,
  RegenerateImageQueued,
  UpdateSocialPostRequest,
} from '../types';

export type { SocialDbDiagnostics, SocialSchemaBootstrapResult };

export const getSocialDbDiagnostics = async (): Promise<SocialDbDiagnostics> => {
  const { data } = await apiClient.get<SocialDbDiagnostics>('/social-posts/db-diagnostics');
  return data;
};

export const ensureSocialSchema = async (): Promise<SocialSchemaBootstrapResult> => {
  const { data } = await apiClient.post<SocialSchemaBootstrapResult>('/social-posts/ensure-schema');
  return data;
};

export const getSocialConfig = async (): Promise<SocialConfig> => {
  const { data } = await apiClient.get<SocialConfig>('/social-posts/config');
  return data;
};

export const testOpenAi = async (): Promise<OpenAiTestResult> => {
  const { data } = await apiClient.post<OpenAiTestResult>('/social-posts/test-openai');
  return data;
};

export const getSocialPosts = async (from?: string, to?: string): Promise<SocialPostsWeek> => {
  const { data } = await apiClient.get<SocialPostsWeek>('/social-posts', {
    params: { from, to },
  });
  return data;
};

export const generateSocialWeek = async (weekStartLocalDate?: string): Promise<SocialPostsWeek> => {
  const { data } = await apiClient.post<SocialPostsWeek>(
    '/social-posts/generate-week',
    { weekStartLocalDate: weekStartLocalDate ?? null },
    { timeout: 120_000 },
  );
  return data;
};

export const updateSocialPost = async (id: string, body: UpdateSocialPostRequest): Promise<SocialPost> => {
  const { data } = await apiClient.put<SocialPost>(`/social-posts/${id}`, body);
  return data;
};

export const scheduleSocialPost = async (id: string): Promise<void> => {
  await apiClient.post(`/social-posts/${id}/schedule`);
};

export interface SocialPostPublishResult {
  success: boolean;
  status: string;
  failureReason?: string | null;
}

export const publishSocialPostNow = async (id: string): Promise<SocialPostPublishResult> => {
  const { data } = await apiClient.post<SocialPostPublishResult>(`/social-posts/${id}/publish-now`);
  return data;
};

export const deleteSocialPost = async (id: string): Promise<void> => {
  await apiClient.delete(`/social-posts/${id}`);
};

export const regeneratePostImage = async (id: string): Promise<RegenerateImageQueued> => {
  const { data } = await apiClient.post<RegenerateImageQueued>(
    `/social-posts/${id}/regenerate-image`,
    {},
    { timeout: 30_000 },
  );
  return data;
};

export const getInstagramStatus = async (): Promise<InstagramConnectionStatus> => {
  const { data } = await apiClient.get<InstagramConnectionStatus>('/social/instagram/status');
  return data;
};

export type InstagramOAuthJobStatus = 'Pending' | 'Connected' | 'Error';

export interface InstagramOAuthJobResult {
  status: InstagramOAuthJobStatus;
  message?: string | null;
}

export const getInstagramOAuthResult = async (): Promise<InstagramOAuthJobResult> => {
  const { data } = await apiClient.get<InstagramOAuthJobResult>('/social/instagram/oauth-result');
  return data;
};

export const getInstagramConnectUrl = async (): Promise<{ url: string }> => {
  const { data } = await apiClient.get<{ url: string }>('/social/instagram/connect-url');
  return data;
};

export const completeInstagramOAuth = async (code: string, state: string): Promise<void> => {
  await apiClient.post('/social/instagram/complete', { code, state });
};

export const disconnectInstagram = async (): Promise<void> => {
  await apiClient.delete('/social/instagram/disconnect');
};

export const getSocialGallery = async (): Promise<import('../types').SocialGalleryImage[]> => {
  const { data } = await apiClient.get<import('../types').SocialGalleryImage[]>('/social/gallery');
  return data ?? [];
};

export const uploadSocialGalleryImage = async (
  file: File,
  captionHint?: string,
): Promise<import('../types').SocialGalleryImage> => {
  const form = new FormData();
  form.append('file', file);
  if (captionHint?.trim()) form.append('captionHint', captionHint.trim());
  const { data } = await apiClient.post<import('../types').SocialGalleryImage>('/social/gallery', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 60_000,
  });
  return data;
};

export const deleteSocialGalleryImage = async (id: string): Promise<void> => {
  await apiClient.delete(`/social/gallery/${id}`);
};

export const createPostFromGallery = async (
  id: string,
  captionHint?: string,
): Promise<import('../types').CreatePostFromGalleryResult> => {
  const { data } = await apiClient.post<import('../types').CreatePostFromGalleryResult>(
    `/social/gallery/${id}/create-post`,
    { captionHint: captionHint ?? null },
    { timeout: 90_000 },
  );
  return data;
};

export const createGalleryAiVariant = async (
  id: string,
  body: import('../types').CreateGalleryAiVariantRequest,
): Promise<import('../types').CreateGalleryAiVariantResult> => {
  const { data } = await apiClient.post<import('../types').CreateGalleryAiVariantResult>(
    `/social/gallery/${id}/ai-variant`,
    body,
    { timeout: 60_000 },
  );
  return data;
};
