import { api } from './api';
import type { Exchange, MatchResult, SkillCategory, User, UserSkill } from '@/types';

// ── Users ─────────────────────────────────────────────────────────────────────

export const userService = {
  getMe: () => api.get<User>('/api/users/me').then((r) => r.data),

  updateProfile: (bio: string) =>
    api.put<User>('/api/users/me', { bio }).then((r) => r.data),

  updateLocation: (latitude: number, longitude: number) =>
    api.put('/api/users/me/location', { latitude, longitude }),

  uploadAvatar: async (uri: string) => {
    const form = new FormData();
    form.append('file', { uri, name: 'avatar.jpg', type: 'image/jpeg' } as any);
    return api.post<{ avatarUrl: string }>('/api/users/me/avatar', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    }).then((r) => r.data);
  },

  getByUsername: (username: string) =>
    api.get<User>(`/api/users/${username}`).then((r) => r.data),
};

// ── Skills ────────────────────────────────────────────────────────────────────

export const skillService = {
  getCategories: () =>
    api.get<SkillCategory[]>('/api/skills/categories').then((r) => r.data),

  getMySkills: () =>
    api.get<UserSkill[]>('/api/skills/mine').then((r) => r.data),

  addSkill: (categoryId: string, type: 'Offer' | 'Seek', level: number, description?: string) =>
    api.post<UserSkill>('/api/skills/mine', { categoryId, type, level, description })
      .then((r) => r.data),

  deleteSkill: (id: string) => api.delete(`/api/skills/mine/${id}`),
};

// ── Matches ───────────────────────────────────────────────────────────────────

export const matchService = {
  getNearby: (lat: number, lng: number, categoryId: string, radiusKm = 10) =>
    api.get<{ results: MatchResult[]; count: number }>('/api/matches/nearby', {
      params: { lat, lng, categoryId, radiusKm },
    }).then((r) => r.data),
};

// ── Exchanges ─────────────────────────────────────────────────────────────────

export const exchangeService = {
  getAll: () =>
    api.get<Exchange[]>('/api/exchanges').then((r) => r.data),

  getById: (id: string) =>
    api.get<Exchange>(`/api/exchanges/${id}`).then((r) => r.data),

  propose: (targetUserId: string, mySkill: string, theirSkill: string) =>
    api.post<Exchange>('/api/exchanges', { targetUserId, mySkill, theirSkill })
      .then((r) => r.data),

  accept: (id: string) =>
    api.put<Exchange>(`/api/exchanges/${id}/accept`).then((r) => r.data),

  complete: (id: string) =>
    api.put<Exchange>(`/api/exchanges/${id}/complete`).then((r) => r.data),

  cancel: (id: string) =>
    api.put<Exchange>(`/api/exchanges/${id}/cancel`).then((r) => r.data),

  postReview: (id: string, score: number, comment?: string) =>
    api.post(`/api/exchanges/${id}/reviews`, { score, comment }),
};
