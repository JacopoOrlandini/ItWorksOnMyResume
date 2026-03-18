import * as SecureStore from 'expo-secure-store';
import { api } from './api';
import type { User } from '@/types';

export const authService = {
  async register(username: string, email: string, password: string) {
    const { data } = await api.post<{ accessToken: string; user: User }>(
      '/api/auth/register',
      { username, email, password }
    );
    await SecureStore.setItemAsync('access_token', data.accessToken);
    return data;
  },

  async login(email: string, password: string) {
    const { data } = await api.post<{ accessToken: string; user: User }>(
      '/api/auth/login',
      { email, password }
    );
    await SecureStore.setItemAsync('access_token', data.accessToken);
    return data;
  },

  async logout() {
    await SecureStore.deleteItemAsync('access_token');
  },

  async getStoredToken() {
    return SecureStore.getItemAsync('access_token');
  },
};
