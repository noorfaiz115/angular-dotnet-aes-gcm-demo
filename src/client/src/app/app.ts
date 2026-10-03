import { Component, signal } from '@angular/core';
import { EncryptedTransport } from './encrypted-transport';

@Component({ selector: 'app-root', templateUrl: './app.html', styleUrl: './app.css' })
export class App {
  protected readonly status = signal('Ready to check the connection.');
  protected readonly busy = signal(false);
  protected readonly wire = signal('');
  protected readonly decrypted = signal('');
  private readonly transport = new EncryptedTransport();

  protected async checkConnection() {
    this.busy.set(true);
    try {
      const response = await fetch('/api/health', { signal: AbortSignal.timeout(15000) });
      if (!response.ok) throw new Error();
      const result = await response.json();
      this.status.set(`${result.service}: ${result.status}. Angular → Gateway → API connected.`);
    } catch { this.status.set('Connection failed. Start API on 5200 and gateway on 5100.'); }
    finally { this.busy.set(false); }
  }
  protected async encryptedEcho(message: string) {
    this.busy.set(true); this.wire.set(''); this.decrypted.set('');
    this.status.set('Establishing session and encrypting...');
    try {
      const result = await this.transport.post('/api/secure/echo', { message });
      this.wire.set(JSON.stringify({ request: result.request, response: result.response }, null, 2));
      this.decrypted.set(JSON.stringify(result.plaintext, null, 2));
      this.status.set('API decrypted the request. Angular authenticated and decrypted the response.');
    } catch (error) { this.status.set(error instanceof Error ? error.message : 'Encrypted round trip failed.'); }
    finally { this.busy.set(false); }
  }
}
