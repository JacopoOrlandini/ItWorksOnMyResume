import { useState } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity, TextInput,
  FlatList, Alert, Image, ActivityIndicator, ScrollView,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '@/store/authStore';
import { userService, skillService } from '@/services/services';
import type { UserSkill } from '@/types';

export default function ProfileScreen() {
  const { user, logout } = useAuthStore();
  const qc = useQueryClient();
  const [bio, setBio] = useState(user?.bio ?? '');
  const [editingBio, setEditingBio] = useState(false);

  const { data: skills, isLoading: skillsLoading } = useQuery({
    queryKey: ['mySkills'],
    queryFn: skillService.getMySkills,
  });

  const updateBioMutation = useMutation({
    mutationFn: () => userService.updateProfile(bio),
    onSuccess: () => { setEditingBio(false); qc.invalidateQueries({ queryKey: ['me'] }); },
    onError: () => Alert.alert('Error', 'Failed to update profile.'),
  });

  const deleteSkillMutation = useMutation({
    mutationFn: (id: string) => skillService.deleteSkill(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['mySkills'] }),
  });

  const pickAvatar = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.Images,
      allowsEditing: true,
      aspect: [1, 1],
      quality: 0.8,
    });
    if (!result.canceled && result.assets[0]) {
      try {
        await userService.uploadAvatar(result.assets[0].uri);
        qc.invalidateQueries({ queryKey: ['me'] });
      } catch {
        Alert.alert('Error', 'Failed to upload avatar.');
      }
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      {/* Avatar */}
      <TouchableOpacity style={styles.avatarContainer} onPress={pickAvatar}>
        {user?.avatarKey ? (
          <Image source={{ uri: user.avatarKey }} style={styles.avatar} />
        ) : (
          <View style={[styles.avatar, styles.avatarPlaceholder]}>
            <Text style={styles.avatarInitial}>
              {user?.username?.[0]?.toUpperCase() ?? '?'}
            </Text>
          </View>
        )}
        <Text style={styles.avatarHint}>Tap to change</Text>
      </TouchableOpacity>

      {/* Username & stats */}
      <Text style={styles.username}>@{user?.username}</Text>
      <Text style={styles.stats}>
        ★ {user?.reputationScore.toFixed(1)} · {user?.exchangesCount} exchanges
      </Text>

      {/* Bio */}
      <View style={styles.section}>
        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>Bio</Text>
          <TouchableOpacity onPress={() => editingBio ? updateBioMutation.mutate() : setEditingBio(true)}>
            <Text style={styles.editLink}>{editingBio ? 'Save' : 'Edit'}</Text>
          </TouchableOpacity>
        </View>
        {editingBio ? (
          <TextInput
            style={styles.bioInput}
            value={bio}
            onChangeText={setBio}
            multiline
            placeholder="Tell people what you can offer…"
          />
        ) : (
          <Text style={styles.bioText}>{bio || 'No bio yet.'}</Text>
        )}
      </View>

      {/* Skills */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>My skills</Text>
        {skillsLoading ? (
          <ActivityIndicator />
        ) : (
          <FlatList
            data={skills ?? []}
            keyExtractor={(s) => s.id}
            scrollEnabled={false}
            renderItem={({ item }) => (
              <SkillRow skill={item} onDelete={() => deleteSkillMutation.mutate(item.id)} />
            )}
            ListEmptyComponent={<Text style={styles.empty}>No skills added yet.</Text>}
          />
        )}
      </View>

      {/* Logout */}
      <TouchableOpacity style={styles.logoutBtn} onPress={logout}>
        <Text style={styles.logoutText}>Sign out</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

function SkillRow({ skill, onDelete }: { skill: UserSkill; onDelete: () => void }) {
  return (
    <View style={styles.skillRow}>
      <View style={styles.skillInfo}>
        <Text style={styles.skillName}>{skill.category}</Text>
        <Text style={styles.skillMeta}>
          {skill.type} · Level {skill.level}/5
        </Text>
        {skill.description && <Text style={styles.skillDesc}>{skill.description}</Text>}
      </View>
      <TouchableOpacity onPress={onDelete}>
        <Text style={styles.deleteBtn}>✕</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container:         { flex: 1, backgroundColor: '#fff' },
  content:           { padding: 24 },
  avatarContainer:   { alignItems: 'center', marginBottom: 12 },
  avatar:            { width: 80, height: 80, borderRadius: 40 },
  avatarPlaceholder: { backgroundColor: '#1a1a1a', justifyContent: 'center', alignItems: 'center' },
  avatarInitial:     { color: '#fff', fontSize: 32, fontWeight: '700' },
  avatarHint:        { fontSize: 12, color: '#999', marginTop: 6 },
  username:          { fontSize: 20, fontWeight: '700', textAlign: 'center', marginBottom: 4 },
  stats:             { fontSize: 14, color: '#666', textAlign: 'center', marginBottom: 24 },
  section:           { marginBottom: 28 },
  sectionHeader:     { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 10 },
  sectionTitle:      { fontSize: 16, fontWeight: '600' },
  editLink:          { fontSize: 14, color: '#6366f1' },
  bioInput:          { borderWidth: 1, borderColor: '#ddd', borderRadius: 8, padding: 10, minHeight: 80, fontSize: 14 },
  bioText:           { fontSize: 14, color: '#444', lineHeight: 22 },
  skillRow:          { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', paddingVertical: 10, borderBottomWidth: 1, borderColor: '#f0f0f0' },
  skillInfo:         { flex: 1 },
  skillName:         { fontSize: 15, fontWeight: '500' },
  skillMeta:         { fontSize: 12, color: '#888', marginTop: 2 },
  skillDesc:         { fontSize: 13, color: '#555', marginTop: 2 },
  deleteBtn:         { color: '#ef4444', fontSize: 16, paddingLeft: 12 },
  empty:             { color: '#999', fontSize: 14 },
  logoutBtn:         { marginTop: 12, padding: 16, borderRadius: 10, borderWidth: 1, borderColor: '#ddd', alignItems: 'center' },
  logoutText:        { fontSize: 15, color: '#ef4444', fontWeight: '500' },
});
