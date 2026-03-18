import { useState, useEffect } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity,
  FlatList, ActivityIndicator, Modal,
} from 'react-native';
import MapView, { Marker, Circle } from 'react-native-maps';
import { useQuery } from '@tanstack/react-query';
import { useLocation } from '@/hooks/useLocation';
import { matchService, skillService } from '@/services/services';
import { userService } from '@/services/services';
import type { MatchResult, SkillCategory } from '@/types';
import { useNavigation } from '@react-navigation/native';

export default function MapScreen() {
  const { coords, loading: locLoading } = useLocation();
  const navigation = useNavigation<any>();
  const [selectedCategory, setSelectedCategory] = useState<SkillCategory | null>(null);
  const [showPicker, setShowPicker] = useState(false);

  // Sync location to backend whenever it changes
  useEffect(() => {
    if (coords) {
      userService.updateLocation(coords.latitude, coords.longitude).catch(() => {});
    }
  }, [coords?.latitude, coords?.longitude]);

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: skillService.getCategories,
  });

  const { data: matchData, isFetching } = useQuery({
    queryKey: ['matches', coords?.latitude, coords?.longitude, selectedCategory?.id],
    queryFn: () => matchService.getNearby(
      coords!.latitude,
      coords!.longitude,
      selectedCategory!.id,
    ),
    enabled: !!coords && !!selectedCategory,
  });

  if (locLoading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator size="large" />
        <Text style={styles.loadingText}>Getting your location…</Text>
      </View>
    );
  }

  const matches = matchData?.results ?? [];

  return (
    <View style={styles.container}>
      {/* Category picker button */}
      <TouchableOpacity style={styles.filterBar} onPress={() => setShowPicker(true)}>
        <Text style={styles.filterText}>
          {selectedCategory ? `Skill: ${selectedCategory.name}` : 'Select a skill to search'}
        </Text>
        <Text style={styles.filterChevron}>▾</Text>
      </TouchableOpacity>

      {/* Map */}
      {coords && (
        <MapView
          style={styles.map}
          initialRegion={{
            latitude:        coords.latitude,
            longitude:       coords.longitude,
            latitudeDelta:   0.05,
            longitudeDelta:  0.05,
          }}
          showsUserLocation
        >
          <Circle
            center={{ latitude: coords.latitude, longitude: coords.longitude }}
            radius={10_000}
            fillColor="rgba(26,26,26,0.05)"
            strokeColor="rgba(26,26,26,0.2)"
            strokeWidth={1}
          />
          {matches.map((m) => (
            <Marker
              key={m.userId}
              coordinate={{ latitude: coords.latitude, longitude: coords.longitude }}
              title={m.username}
              description={`${m.distanceKm} km · ★ ${m.reputationScore.toFixed(1)}`}
              onCalloutPress={() => navigation.navigate('UserProfile', { userId: m.userId })}
            />
          ))}
        </MapView>
      )}

      {/* Results list */}
      {isFetching && (
        <ActivityIndicator style={styles.listLoader} />
      )}
      <FlatList
        data={matches}
        keyExtractor={(m) => m.userId}
        style={styles.list}
        renderItem={({ item }) => (
          <MatchCard match={item} onPress={() =>
            navigation.navigate('UserProfile', { userId: item.userId })
          } />
        )}
        ListEmptyComponent={
          selectedCategory && !isFetching
            ? <Text style={styles.empty}>No matches found nearby.</Text>
            : null
        }
      />

      {/* Category picker modal */}
      <Modal visible={showPicker} animationType="slide" presentationStyle="pageSheet">
        <View style={styles.modal}>
          <Text style={styles.modalTitle}>Choose a skill</Text>
          <FlatList
            data={categories}
            keyExtractor={(c) => c.id}
            renderItem={({ item }) => (
              <TouchableOpacity
                style={styles.categoryRow}
                onPress={() => { setSelectedCategory(item); setShowPicker(false); }}
              >
                <Text style={styles.categoryName}>{item.name}</Text>
                {selectedCategory?.id === item.id && <Text>✓</Text>}
              </TouchableOpacity>
            )}
          />
          <TouchableOpacity style={styles.closeBtn} onPress={() => setShowPicker(false)}>
            <Text style={styles.closeBtnText}>Cancel</Text>
          </TouchableOpacity>
        </View>
      </Modal>
    </View>
  );
}

function MatchCard({ match, onPress }: { match: MatchResult; onPress: () => void }) {
  return (
    <TouchableOpacity style={styles.card} onPress={onPress}>
      <View style={styles.cardRow}>
        <Text style={styles.cardName}>{match.username}</Text>
        <Text style={styles.cardDist}>{match.distanceKm.toFixed(1)} km</Text>
      </View>
      <Text style={styles.cardSkill}>{match.skill.description}</Text>
      <Text style={styles.cardRep}>★ {match.reputationScore.toFixed(1)} · {match.exchangesCount} exchanges</Text>
    </TouchableOpacity>
  );
}

const styles = StyleSheet.create({
  container:    { flex: 1, backgroundColor: '#fff' },
  center:       { flex: 1, justifyContent: 'center', alignItems: 'center' },
  loadingText:  { marginTop: 12, color: '#666' },
  filterBar:    { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 14, borderBottomWidth: 1, borderColor: '#eee' },
  filterText:   { fontSize: 15, color: '#1a1a1a' },
  filterChevron: { fontSize: 16, color: '#999' },
  map:          { height: 260 },
  listLoader:   { marginTop: 8 },
  list:         { flex: 1 },
  empty:        { padding: 24, color: '#999', textAlign: 'center' },
  card:         { padding: 16, borderBottomWidth: 1, borderColor: '#f0f0f0' },
  cardRow:      { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 4 },
  cardName:     { fontSize: 15, fontWeight: '600' },
  cardDist:     { fontSize: 13, color: '#888' },
  cardSkill:    { fontSize: 13, color: '#444', marginBottom: 2 },
  cardRep:      { fontSize: 12, color: '#888' },
  modal:        { flex: 1, padding: 24 },
  modalTitle:   { fontSize: 20, fontWeight: '600', marginBottom: 16 },
  categoryRow:  { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 14, borderBottomWidth: 1, borderColor: '#f0f0f0' },
  categoryName: { fontSize: 16 },
  closeBtn:     { marginTop: 24, padding: 16, alignItems: 'center', borderRadius: 10, backgroundColor: '#f0f0f0' },
  closeBtnText: { fontSize: 16, fontWeight: '600' },
});
