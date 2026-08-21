import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PasswordAssessmentController } from '../../controllers/password-assessment.controller';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-password-assessment',
  templateUrl: './password-assessment.html',
})
export class PasswordAssessmentView {
  protected readonly controller = inject(PasswordAssessmentController);

  protected onPasswordInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.controller.updatePassword(input.value);
  }
}
