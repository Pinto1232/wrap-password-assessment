import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PasswordAssessmentView } from './views/password-assessment/password-assessment';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PasswordAssessmentView],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {}
