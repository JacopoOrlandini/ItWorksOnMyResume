import { useEffect } from 'react';
import * as Notifications from 'expo-notifications';
import { useAuthStore } from '@/store/authStore';

const NTFY_URL = process.env.EXPO_PUBLIC_NTFY_URL ?? 'http://localhost:8090';

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

export function useNotifications() {
  const user = useAuthStore((s) => s.user);

  useEffect(() => {
    if (!user) return;

    Notifications.requestPermissionsAsync();

    const topic = `user-${user.id.replace(/-/g, '')}`;
    const url   = `${NTFY_URL}/${topic}/sse`;
    let active  = true;
    let reader: ReadableStreamDefaultReader<Uint8Array> | null = null;

    const listen = async () => {
      try {
        const response = await fetch(url);
        if (!response.body) return;

        reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (active) {
          const { value, done } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            if (!line.startsWith('data:')) continue;
            const raw = line.slice(5).trim();
            if (!raw || raw === 'keepalive') continue;
            try {
              const payload = JSON.parse(raw);
              await Notifications.scheduleNotificationAsync({
                content: {
                  title: payload.title ?? 'ItWorksOnMyResume',
                  body:  payload.message ?? '',
                },
                trigger: null,
              });
            } catch {
              // non-JSON line — ignore
            }
          }
        }
      } catch {
        if (active) setTimeout(listen, 5000);
      }
    };

    listen();

    return () => {
      active = false;
      reader?.cancel();
    };
  }, [user?.id]);
}