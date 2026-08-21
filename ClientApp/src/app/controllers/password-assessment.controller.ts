import { computed, Injectable, signal } from '@angular/core';
import { assessPassword } from '../models/password-assessment.model';

@Injectable({ providedIn: 'root' })
export class PasswordAssessmentController {
  private readonly passwordState = signal('');
  private readonly passwordVisibilityState = signal(false);

  readonly password = this.passwordState.asReadonly();
  readonly isPasswordVisible = this.passwordVisibilityState.asReadonly();
  readonly assessment = computed(() => assessPassword(this.passwordState()));

  updatePassword(password: string): void {
    this.passwordState.set(password);
  }

  togglePasswordVisibility(): void {
    this.passwordVisibilityState.update((isVisible) => !isVisible);
  }

  clearPassword(): void {
    this.passwordState.set('');
    this.passwordVisibilityState.set(false);
  }
}
