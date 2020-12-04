import jwtDecode from 'jwt-decode';

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
}
