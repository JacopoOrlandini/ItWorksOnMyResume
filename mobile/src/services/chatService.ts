import * as signalR from '@microsoft/signalr';
import * as SecureStore from 'expo-secure-store';

const BASE_URL = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5000';

export function createChatConnection(exchangeId: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${BASE_URL}/hubs/chat?exchangeId=${exchangeId}`, {
      accessTokenFactory: async () =>
        (await SecureStore.getItemAsync('access_token')) ?? '',
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}
