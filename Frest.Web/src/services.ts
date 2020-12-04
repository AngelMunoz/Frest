import jwtDecode from 'jwt-decode';
import axios, { AxiosResponse } from 'axios';

const $http = axios.create({
  baseURL: location.host.includes('localhost')
    ? 'http://localhost:5000'
    : 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
});
const interceptors = new Map<string, number>();

let appState: AppContext = {
  authentication: { isAuthenticated: false },
};

export class AuthService {
  static validateAuth() {
    const token = localStorage.getItem('access_token');
    if (!token) return false;
    const decoded = jwtDecode<any>(token);
    const now = Date.now().valueOf() / 1000;
    if (typeof decoded.exp !== 'undefined' && decoded.exp < now) {
      console.warn(`token expired: ${JSON.stringify(decoded)}`);
      return false;
    }
    if (typeof decoded.nbf !== 'undefined' && decoded.nbf > now) {
      console.warn(`token not yet valid: ${JSON.stringify(decoded)}`);
      return false;
    }
    return true;
  }

  static get isAuthenticated() {
    const isAuthenticated = AuthService.validateAuth();
    AuthService.setAuth({ isAuthenticated: isAuthenticated });
    return appState.authentication.isAuthenticated;
  }

  static setAuth(auth: AuthState) {
    appState = { ...appState, authentication: { ...auth } };
  }

  static async login(payload: LoginPayload): Promise<AuthResponse> {
    try {
      var { data, status } = await $http.post<
        LoginPayload,
        AxiosResponse<AuthResponse>
      >('/api/auth/login', payload);
      if (status !== 200) return Promise.reject(data as { message: string });
      const { token } = data as { token: string };
      interceptors.set(
        'bearer',
        $http.interceptors.request.use((request) => {
          request.headers = {
            ...request.headers,
            Authorization: `Bearer ${token}`,
          };
          return request;
        }),
      );
      return data;
    } catch (error) {
      return Promise.reject(error);
    }
  }

  static async signup(payload: SignupPayload) {
    try {
      var { data, status } = await $http.post<
        SignupPayload,
        AxiosResponse<AuthResponse>
      >('/api/auth/signup', payload);
      if (status !== 200) return Promise.reject(data as { message: string });
      const { token } = data as { token: string };
      interceptors.set(
        'bearer',
        $http.interceptors.request.use((request) => {
          request.headers = {
            ...request.headers,
            Authorization: `Bearer ${token}`,
          };
          return request;
        }),
      );
      return data;
    } catch (error) {
      return Promise.reject(error);
    }
  }

  static logout() {
    localStorage.clear();
    interceptors.clear();
    window.location.reload();
  }
}
