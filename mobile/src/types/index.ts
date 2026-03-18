export interface User {
  id: string;
  username: string;
  email: string;
  bio?: string;
  avatarKey?: string;
  reputationScore: number;
  exchangesCount: number;
  createdAt: string;
}

export interface SkillCategory {
  id: string;
  name: string;
  slug: string;
  icon?: string;
}

export interface UserSkill {
  id: string;
  categoryId: string;
  category?: string;
  type: 'Offer' | 'Seek';
  level: number;  // 1–5
  description?: string;
  createdAt: string;
}

export interface MatchResult {
  userId: string;
  username: string;
  avatarUrl?: string;
  bio?: string;
  reputationScore: number;
  exchangesCount: number;
  distanceKm: number;
  skill: {
    description: string;
    level: number;
  };
}

export type ExchangeStatus = 'Pending' | 'Accepted' | 'Completed' | 'Cancelled';

export interface Exchange {
  id: string;
  userA: { id: string; username: string };
  userB: { id: string; username: string };
  skillA: string;
  skillB: string;
  status: ExchangeStatus;
  proposedAt: string;
  acceptedAt?: string;
  completedAt?: string;
  messages: Message[];
}

export interface Message {
  id: string;
  senderId: string;
  body: string;
  sentAt: string;
}
