import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-auth-page', imports: [FormsModule, RouterLink], templateUrl: './auth-page.html', styleUrl: './auth-page.css'
})
export class AuthPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly register = inject(ActivatedRoute).snapshot.data['register'] === true;
  readonly busy = signal(false);
  readonly message = signal('');
  readonly success = signal(false);
  name = ''; email = ''; password = ''; confirmPassword = '';
  async submit() {
    if (this.busy()) return;
    this.message.set(''); this.success.set(false);
    if (this.register && this.password !== this.confirmPassword) { this.message.set('Passwords must match.'); return; }
    this.busy.set(true);
    try {
      if (this.register) {
        this.message.set(await this.auth.register(this.name, this.email, this.password));
        this.success.set(true);
      } else {
        await this.auth.login(this.email, this.password);
        await this.router.navigateByUrl('/profile');
      }
    } catch (error) { this.message.set(error instanceof Error ? error.message : 'Unable to complete authentication.'); }
    finally { this.password = ''; this.confirmPassword = ''; this.busy.set(false); }
  }
}
