export type PasswordStrengthTone = 'empty' | 'weak' | 'fair' | 'good' | 'strong' | 'excellent';

export interface PasswordCriterionResult {
  readonly id: string;
  readonly label: string;
  readonly isMet: boolean;
  readonly feedback: string;
}

export interface PasswordAssessment {
  readonly score: number;
  readonly maxScore: number;
  readonly progress: number;
  readonly isEmpty: boolean;
  readonly criteria: readonly PasswordCriterionResult[];
  readonly label: string;
  readonly tone: PasswordStrengthTone;
  readonly feedback: string;
}

interface PasswordCriterionDefinition {
  readonly id: string;
  readonly label: string;
  readonly feedback: string;
  readonly test: (password: string) => boolean;
}

const criteria: readonly PasswordCriterionDefinition[] = [
  {
    id: 'length',
    label: 'At least 12 characters',
    feedback: 'Use at least 12 characters.',
    test: (password) => password.length >= 12,
  },
  {
    id: 'lowercase',
    label: 'One lowercase letter',
    feedback: 'Add a lowercase letter.',
    test: (password) => /[a-z]/.test(password),
  },
  {
    id: 'uppercase',
    label: 'One uppercase letter',
    feedback: 'Add an uppercase letter.',
    test: (password) => /[A-Z]/.test(password),
  },
  {
    id: 'number',
    label: 'One number',
    feedback: 'Add a number.',
    test: (password) => /\d/.test(password),
  },
  {
    id: 'symbol',
    label: 'One symbol',
    feedback: 'Add a symbol or punctuation mark.',
    test: (password) => /[^A-Za-z0-9\s]/.test(password),
  },
  {
    id: 'spaces',
    label: 'No whitespace',
    feedback: 'Remove spaces and other whitespace.',
    test: (password) => password.length > 0 && !/\s/.test(password),
  },
];

function strengthFor(
  score: number,
  isEmpty: boolean,
): { readonly label: string; readonly tone: PasswordStrengthTone } {
  if (isEmpty) return { label: 'Not assessed', tone: 'empty' };
  if (score <= 2) return { label: 'Weak', tone: 'weak' };
  if (score === 3) return { label: 'Fair', tone: 'fair' };
  if (score === 4) return { label: 'Good', tone: 'good' };
  if (score === 5) return { label: 'Strong', tone: 'strong' };
  return { label: 'Excellent', tone: 'excellent' };
}

export function assessPassword(value = ''): PasswordAssessment {
  const results = criteria.map((criterion) => ({
    id: criterion.id,
    label: criterion.label,
    isMet: criterion.test(value),
    feedback: criterion.feedback,
  }));
  const score = results.filter((criterion) => criterion.isMet).length;
  const maxScore = criteria.length;
  const isEmpty = value.length === 0;
  const strength = strengthFor(score, isEmpty);
  const nextStep = results.find((criterion) => !criterion.isMet);

  return {
    score,
    maxScore,
    progress: isEmpty ? 0 : Math.round((score / maxScore) * 100),
    isEmpty,
    criteria: results,
    label: strength.label,
    tone: strength.tone,
    feedback: isEmpty
      ? 'Enter a password to see a local composition check.'
      : (nextStep?.feedback ?? 'This password meets every composition check.'),
  };
}
