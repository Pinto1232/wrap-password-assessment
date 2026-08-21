import { TestBed } from '@angular/core/testing';
import { PasswordAssessmentController } from './password-assessment.controller';

describe('PasswordAssessmentController', () => {
  let controller: PasswordAssessmentController;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    controller = TestBed.inject(PasswordAssessmentController);
  });

  it('coordinates input and assessment state', () => {
    controller.updatePassword('Correct-Horse-7');

    expect(controller.password()).toBe('Correct-Horse-7');
    expect(controller.assessment().label).toBe('Excellent');
  });

  it('clears both the password and visibility state', () => {
    controller.updatePassword('sample');
    controller.togglePasswordVisibility();
    controller.clearPassword();

    expect(controller.password()).toBe('');
    expect(controller.isPasswordVisible()).toBe(false);
  });
});
