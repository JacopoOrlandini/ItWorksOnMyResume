import { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity,
  StyleSheet, Alert, KeyboardAvoidingView, Platform,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useAuthStore } from '@/store/authStore';

export default function LoginScreen() {
  const [email, setEmail]       = useState('');
  const [password, setPassword] = useState('');
  const { login, isLoading }    = useAuthStore();
  const navigation              = useNavigation<any>();

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert('Error', 'Please fill in all fields.');
      return;
    }
    try {
      await login(email.trim(), password);
      // Navigation handled by root navigator watching auth state
    } catch (e: any) {
      const msg = e.response?.status === 401
        ? 'Invalid email or password.'
        : 'Login failed. Check your connection.';
      Alert.alert('Error', msg);
    }
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <Text style={styles.title}>ItWorksOnMyResume</Text>
      <Text style={styles.subtitle}>Trade what you know for what you need</Text>

      <TextInput
        style={styles.input}
        placeholder="Email"
        placeholderTextColor="#999"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />
      <TextInput
        style={styles.input}
        placeholder="Password"
        placeholderTextColor="#999"
        secureTextEntry
        value={password}
        onChangeText={setPassword}
      />

      <TouchableOpacity
        style={[styles.button, isLoading && styles.buttonDisabled]}
        onPress={handleLogin}
        disabled={isLoading}
      >
        <Text style={styles.buttonText}>{isLoading ? 'Signing in…' : 'Sign in'}</Text>
      </TouchableOpacity>

      <TouchableOpacity onPress={() => navigation.navigate('Register')}>
        <Text style={styles.link}>Don't have an account? Register</Text>
      </TouchableOpacity>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container:      { flex: 1, justifyContent: 'center', padding: 24, backgroundColor: '#fff' },
  title:          { fontSize: 26, fontWeight: '700', textAlign: 'center', marginBottom: 6 },
  subtitle:       { fontSize: 14, color: '#666', textAlign: 'center', marginBottom: 36 },
  input:          { borderWidth: 1, borderColor: '#ddd', borderRadius: 10, padding: 14, marginBottom: 14, fontSize: 16 },
  button:         { backgroundColor: '#1a1a1a', borderRadius: 10, padding: 16, alignItems: 'center', marginBottom: 16 },
  buttonDisabled: { opacity: 0.5 },
  buttonText:     { color: '#fff', fontSize: 16, fontWeight: '600' },
  link:           { color: '#555', textAlign: 'center', fontSize: 14 },
});
