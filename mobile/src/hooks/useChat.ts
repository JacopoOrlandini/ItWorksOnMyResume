import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { createChatConnection } from '@/services/chatService';
import type { Message } from '@/types';

export function useChat(exchangeId: string, initialMessages: Message[] = []) {
  const [messages, setMessages] = useState<Message[]>(initialMessages);
  const [connected, setConnected] = useState(false);
  const connRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const conn = createChatConnection(exchangeId);
    connRef.current = conn;

    conn.on('ReceiveMessage', (msg: Message) => {
      setMessages((prev) => [...prev, msg]);
    });

    conn.onreconnecting(() => setConnected(false));
    conn.onreconnected(() => setConnected(true));
    conn.onclose(() => setConnected(false));

    conn.start()
      .then(() => setConnected(true))
      .catch((e) => console.warn('Chat connection failed:', e));

    return () => { conn.stop(); };
  }, [exchangeId]);

  const sendMessage = useCallback(async (body: string) => {
    if (connRef.current?.state === signalR.HubConnectionState.Connected) {
      await connRef.current.invoke('SendMessage', body);
    }
  }, []);

  const sendTyping = useCallback(async () => {
    if (connRef.current?.state === signalR.HubConnectionState.Connected) {
      await connRef.current.invoke('Typing');
    }
  }, []);

  return { messages, connected, sendMessage, sendTyping };
}
