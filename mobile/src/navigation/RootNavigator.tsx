import { useEffect } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Text } from 'react-native';
import { useAuthStore } from '@/store/authStore';
import { authService } from '@/services/authService';
import { userService } from '@/services/services';
import { useNotifications } from '@/hooks/useNotifications';

import LoginScreen from '@/screens/auth/LoginScreen';
import RegisterScreen from '@/screens/auth/RegisterScreen';
import MapScreen from '@/screens/map/MapScreen';
import ProfileScreen from '@/screens/profile/ProfileScreen';
import { ExchangeListScreen, ChatScreen } from '@/screens/exchange/ExchangeScreens';

const Stack = createNativeStackNavigator();
const Tab   = createBottomTabNavigator();

function TabNavigator() {
  return (
    <Tab.Navigator
      screenOptions={{
        tabBarActiveTintColor: '#1a1a1a',
        headerShown: true,
      }}
    >
      <Tab.Screen
        name="Map"
        component={MapScreen}
        options={{ tabBarLabel: 'Discover', tabBarIcon: () => <Text>🗺</Text> }}
      />
      <Tab.Screen
        name="Exchanges"
        component={ExchangeListScreen}
        options={{ tabBarLabel: 'Exchanges', tabBarIcon: () => <Text>🤝</Text> }}
      />
      <Tab.Screen
        name="Profile"
        component={ProfileScreen}
        options={{ tabBarLabel: 'Profile', tabBarIcon: () => <Text>👤</Text> }}
      />
    </Tab.Navigator>
  );
}

export default function RootNavigator() {
  const { user, setUser } = useAuthStore();
  useNotifications();

  // Restore session on app launch
  useEffect(() => {
    (async () => {
      const token = await authService.getStoredToken();
      if (token) {
        try {
          const me = await userService.getMe();
          setUser(me);
        } catch {
          // Token expired — stay on login screen
        }
      }
    })();
  }, []);

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {user ? (
          <>
            <Stack.Screen name="Main" component={TabNavigator} />
            <Stack.Screen
              name="Chat"
              component={ChatScreen}
              options={{ headerShown: true, title: 'Exchange chat' }}
            />
            <Stack.Screen
              name="UserProfile"
              component={ProfileScreen}
              options={{ headerShown: true, title: 'Profile' }}
            />
          </>
        ) : (
          <>
            <Stack.Screen name="Login"    component={LoginScreen} />
            <Stack.Screen name="Register" component={RegisterScreen} />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}
