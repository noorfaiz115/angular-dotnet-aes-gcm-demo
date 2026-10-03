import { Injectable, signal } from '@angular/core';
import { EncryptedTransport } from './encrypted-transport';

export interface User { id: string; name: string; email: string; }
interface AuthResult { ok: boolean; message: string; user?: User; token?: string; }
@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user = signal<User | null>(null);
  private token?: string;
  private readonly transport = new EncryptedTransport();
  private async call(operation: string, payload: unknown): Promise<AuthResult> {
    const result = await this.transport.post('/api/auth/' + operation, payload);
    // Authentication payloads never enter the lab's debug panels or browser storage.
    return result.plaintext as AuthResult;
  }
  async register(name: string, email: string, password: string) {
    const result = await this.call('register', { name, email, password });
    if (!result.ok) throw new Error(result.message);
    return result.message;
  }
  async login(email: string, password: string) {
    const result = await this.call('login', { email, password });
    if (!result.ok || !result.token || !result.user) throw new Error(result.message);
    this.token = result.token;
    this.user.set(result.user);
  }
  async profile() {
    const result = await this.call('me', { token: this.token });
    if (!result.ok || !result.user) { this.token = undefined; this.user.set(null); throw new Error(result.message); }
    this.user.set(result.user);
  }
  async logout() {
    const result = await this.call('logout', { token: this.token });
    this.token = undefined; this.user.set(null);
    if (!result.ok) throw new Error(result.message);
  }
}
