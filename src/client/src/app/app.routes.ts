import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { AuthService } from './auth.service';
import { AuthPage } from './auth-page';
import { Lab } from './lab';
import { Profile } from './profile';
const loggedIn: CanActivateFn = () => inject(AuthService).user() ? true : inject(Router).createUrlTree(['/login']);
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: AuthPage },
  { path: 'register', component: AuthPage, data: { register: true } },
  { path: 'profile', component: Profile, canActivate: [loggedIn] },
  { path: 'lab', component: Lab },
  { path: '**', redirectTo: 'login' }
];
