import { useState, useRef, useEffect } from 'react';
import {
  View, Text, FlatList, TouchableOpacity, TextInput,
  StyleSheet, ActivityIndicator, KeyboardAvoidingView, Platform,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigation, useRoute } from '@react-navigation/native';
import { exchangeService } from '@/services/services';
import { useChat } from '@/hooks/useChat';
import { useAuthStore } from '@/store/authStore';
import type { Exchange } from '@/types';

// ── Exchange list ─────────────────────────────────────────────────────────────

export function ExchangeListScreen() {
  const navigation = useNavigation<any>();
  const { data: exchanges, isLoading } = useQuery({
    queryKey: ['exchanges'],
    queryFn: exchangeService.getAll,
  });

  if (isLoading) return <ActivityIndicator style={{ flex: 1 }} />;

  return (
    <View style={styles.container}>
      <FlatList
        data={exchanges ?? []}
        keyExtractor={(e) => e.id}
        renderItem={({ item }) => (
          <ExchangeRow
            exchange={item}
            onPress={() => navigation.navigate('Chat', { exchangeId: item.id })}
          />
        )}
        ListEmptyComponent={
          <Text style={styles.empty}>No exchanges yet. Find someone on the map!</Text>
        }
      />
    </View>
  );
}

function ExchangeRow({ exchange, onPress }: { exchange: Exchange; onPress: () => void }) {
  const statusColor: Record<string, string> = {
    Pending:   '#f59e0b',
    Accepted:  '#10b981',
    Completed: '#6b7280',
    Cancelled: '#ef4444',
  };
  return (
    <TouchableOpacity style={styles.row} onPress={onPress}>
      <View style={styles.rowTop}>
        <Text style={styles.rowName}>
          {exchange.userA.username} ↔ {exchange.userB.username}
        </Text>
        <Text style={[styles.badge, { color: statusColor[exchange.status] ?? '#000' }]}>
          {exchange.status}
        </Text>
      </View>
      <Text style={styles.rowSkills}>{exchange.skillA} → {exchange.skillB}</Text>
    </TouchableOpacity>
  );
}

// ── Chat screen ───────────────────────────────────────────────────────────────

export function ChatScreen() {
  const route        = useRoute<any>();
  const { exchangeId } = route.params as { exchangeId: string };
  const user         = useAuthStore((s) => s.user);
  const qc           = useQueryClient();
  const listRef      = useRef<FlatList>(null);
  const [text, setText] = useState('');

  const { data: exchange, isLoading } = useQuery({
    queryKey: ['exchange', exchangeId],
    queryFn: () => exchangeService.getById(exchangeId),
  });

  const { messages, connected, sendMessage } = useChat(
    exchangeId,
    exchange?.messages ?? []
  );

  const acceptMutation = useMutation({
    mutationFn: () => exchangeService.accept(exchangeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exchange', exchangeId] }),
  });

  const completeMutation = useMutation({
    mutationFn: () => exchangeService.complete(exchangeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['exchange', exchangeId] }),
  });

  useEffect(() => {
    if (messages.length > 0) {
      setTimeout(() => listRef.current?.scrollToEnd({ animated: true }), 100);
    }
  }, [messages.length]);

  const handleSend = async () => {
    const body = text.trim();
    if (!body) return;
    setText('');
    await sendMessage(body);
  };

  if (isLoading || !exchange) return <ActivityIndicator style={{ flex: 1 }} />;

  const canAccept   = exchange.status === 'Pending' && exchange.userB.id === user?.id;
  const canComplete = exchange.status === 'Accepted';

  return (
    <KeyboardAvoidingView
      style={{ flex: 1 }}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      keyboardVerticalOffset={90}
    >
      {/* Header info */}
      <View style={styles.chatHeader}>
        <Text style={styles.chatHeaderText}>
          {exchange.skillA} ↔ {exchange.skillB}
        </Text>
        <Text style={[styles.chatStatus, { color: connected ? '#10b981' : '#f59e0b' }]}>
          {connected ? 'Connected' : 'Reconnecting…'}
        </Text>
      </View>

      {/* Action buttons */}
      {canAccept && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#10b981' }]}
          onPress={() => acceptMutation.mutate()}
          disabled={acceptMutation.isPending}
        >
          <Text style={styles.actionBtnText}>Accept exchange</Text>
        </TouchableOpacity>
      )}
      {canComplete && (
        <TouchableOpacity
          style={[styles.actionBtn, { backgroundColor: '#6366f1' }]}
          onPress={() => completeMutation.mutate()}
          disabled={completeMutation.isPending}
        >
          <Text style={styles.actionBtnText}>Mark as completed</Text>
        </TouchableOpacity>
      )}

      {/* Messages */}
      <FlatList
        ref={listRef}
        data={messages}
        keyExtractor={(m) => m.id}
        style={styles.messageList}
        renderItem={({ item }) => (
          <MessageBubble
            body={item.body}
            sentAt={item.sentAt}
            isOwn={item.senderId === user?.id}
          />
        )}
      />

      {/* Input */}
      {exchange.status === 'Accepted' && (
        <View style={styles.inputRow}>
          <TextInput
            style={styles.messageInput}
            placeholder="Type a message…"
            value={text}
            onChangeText={setText}
            multiline
          />
          <TouchableOpacity style={styles.sendBtn} onPress={handleSend}>
            <Text style={styles.sendBtnText}>Send</Text>
          </TouchableOpacity>
        </View>
      )}
    </KeyboardAvoidingView>
  );
}

function MessageBubble({ body, sentAt, isOwn }: { body: string; sentAt: string; isOwn: boolean }) {
  return (
    <View style={[styles.bubble, isOwn ? styles.bubbleOwn : styles.bubbleOther]}>
      <Text style={[styles.bubbleText, isOwn && styles.bubbleTextOwn]}>{body}</Text>
      <Text style={styles.bubbleTime}>
        {new Date(sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container:       { flex: 1, backgroundColor: '#fff' },
  empty:           { padding: 32, textAlign: 'center', color: '#999' },
  row:             { padding: 16, borderBottomWidth: 1, borderColor: '#f0f0f0' },
  rowTop:          { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  rowName:         { fontSize: 15, fontWeight: '600' },
  badge:           { fontSize: 12, fontWeight: '500' },
  rowSkills:       { fontSize: 13, color: '#666' },
  chatHeader:      { padding: 12, borderBottomWidth: 1, borderColor: '#eee', flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  chatHeaderText:  { fontSize: 14, fontWeight: '600' },
  chatStatus:      { fontSize: 12 },
  actionBtn:       { margin: 10, padding: 12, borderRadius: 8, alignItems: 'center' },
  actionBtnText:   { color: '#fff', fontWeight: '600' },
  messageList:     { flex: 1, padding: 12 },
  bubble:          { maxWidth: '75%', padding: 10, borderRadius: 14, marginBottom: 8 },
  bubbleOwn:       { backgroundColor: '#1a1a1a', alignSelf: 'flex-end' },
  bubbleOther:     { backgroundColor: '#f0f0f0', alignSelf: 'flex-start' },
  bubbleText:      { fontSize: 15, color: '#1a1a1a' },
  bubbleTextOwn:   { color: '#fff' },
  bubbleTime:      { fontSize: 10, color: '#aaa', marginTop: 3, textAlign: 'right' },
  inputRow:        { flexDirection: 'row', padding: 10, borderTopWidth: 1, borderColor: '#eee', alignItems: 'flex-end' },
  messageInput:    { flex: 1, borderWidth: 1, borderColor: '#ddd', borderRadius: 20, paddingHorizontal: 14, paddingVertical: 8, maxHeight: 100, fontSize: 15 },
  sendBtn:         { marginLeft: 8, backgroundColor: '#1a1a1a', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 10 },
  sendBtnText:     { color: '#fff', fontWeight: '600' },
});
