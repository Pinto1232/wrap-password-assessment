import { assessPassword } from './password-assessment.model';

describe('assessPassword', () => {
  it('returns an empty assessment before input', () => {
    const assessment = assessPassword();

    expect(assessment.label).toBe('Not assessed');
    expect(assessment.score).toBe(0);
    expect(assessment.progress).toBe(0);
  });

  it('identifies a password that meets every composition rule', () => {
    const assessment = assessPassword('Correct-Horse-7');

    expect(assessment.label).toBe('Excellent');
    expect(assessment.score).toBe(assessment.maxScore);
    expect(assessment.progress).toBe(100);
  });

  it('returns the next unmet rule as feedback', () => {
    const assessment = assessPassword('short');

    expect(assessment.label).toBe('Weak');
    expect(assessment.feedback).toBe('Use at least 12 characters.');
  });
});
