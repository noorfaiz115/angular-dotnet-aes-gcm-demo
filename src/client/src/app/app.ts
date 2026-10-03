import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly status = signal('Ready to check the connection.');
  protected readonly busy = signal(false);

  protected async checkConnection() {
    this.busy.set(true);
    this.status.set('Connecting through the gateway...');
    try {
      const response = await fetch('/api/health', { signal: AbortSignal.timeout(15000) });
      if (!response.ok) throw new Error(`Gateway returned HTTP ${response.status}`);
      const result = await response.json();
      this.status.set(`${result.service}: ${result.status}. Angular → Gateway → API connected.`);
    } catch {
      this.status.set('Connection failed. Start the API on 5200 and gateway on 5100.');
    } finally {
      this.busy.set(false);
    }
  }
}
