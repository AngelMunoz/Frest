import './fre-home.js';
import './fre-profile.js';
import './fre-places.js';

import { Commands, Context, Router } from '@vaadin/router';
import { AuthService } from './services.js';

const outlet = document.querySelector('#outlet');
const router = new Router(outlet);

router.setRoutes([
  { path: '/', component: 'fre-home', name: 'home' },
  {
    path: '/profile',
    action: async (context: Context, commands: Commands) => {
      if (!AuthService.isAuthenticated) {
        return commands.redirect('/');
      }
      return commands.component('fre-profile');
    },
    name: 'profile',
  },
  {
    path: '/places',
    action: async (context: Context, commands: Commands) => {
      if (!AuthService.isAuthenticated) {
        return commands.redirect('/');
      }
      return commands.component('fre-places');
    },
    component: 'fre-places',
    name: 'places',
  },
]);

if (import.meta.hot) {
  import.meta.hot.accept();
}
