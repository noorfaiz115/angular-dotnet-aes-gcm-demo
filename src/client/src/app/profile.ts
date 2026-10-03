import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({ selector: 'app-profile', template: `
<section class="card">
  <span class="badge">PROTECTED PROFILE</span><h1>Hello, {{ auth.user()?.name }}</h1>
  <p>{{ auth.user()?.email }}</p>
  <p>Your login token stays in browser memory. Profile requests and responses use AES-GCM.</p>
  <button type="button" (click)="refresh()" [disabled]="busy()">Verify profile with API</button>
  <button type="button" (click)="logout()" [disabled]="busy()">Log out</button>
  <p role="status">{{ message() }}</p>
  <aside>Refreshing the browser clears your login. API restart clears demo users.</aside>
</section>`, styleUrl: './auth-page.css' })
export class Profile {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly busy = signal(false);
  readonly message = signal('');
  constructor() { void this.refresh(); }
  async refresh() {
    this.busy.set(true);
    try { await this.auth.profile(); this.message.set('API verified your login and returned your profile.'); }
    catch (error) {
      this.message.set(error instanceof Error ? error.message : 'Profile check failed.');
      if (!this.auth.user()) await this.router.navigateByUrl('/login');
    } finally { this.busy.set(false); }
  }
  async logout() {
    this.busy.set(true);
    try { await this.auth.logout(); await this.router.navigateByUrl('/login'); }
    catch (error) {
      this.message.set(error instanceof Error ? error.message : 'Logout failed.');
      if (!this.auth.user()) await this.router.navigateByUrl('/login');
    } finally { this.busy.set(false); }
  }
}
