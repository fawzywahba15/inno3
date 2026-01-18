import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SearchComponent } from '../search/search.component';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    MatToolbarModule,
    MatIconModule,
    SearchComponent
  ],
  styleUrl: './app.component.css',
  template: `
    <mat-toolbar color="primary" style="display: flex; justify-content: space-between;">
      <span>
        <mat-icon style="vertical-align: middle; margin-right: 8px">school</mat-icon>
        Moodle Search (CIS Integrated)
      </span>

      <div style="font-size: 14px;">
        <span *ngIf="auth.isLecturer()" style="background: white; color: #3f51b5; padding: 4px 8px; border-radius: 4px; font-weight: bold;">
          LEHRENDER
        </span>
        <span *ngIf="!auth.isLecturer()" style="opacity: 0.8;">
          Studierender
        </span>
      </div>
    </mat-toolbar>

    <div style="padding: 20px;">
      <app-search></app-search>
    </div>
  `
})
export class AppComponent {
  title = 'inno3';

  // Auth Service injizieren, damit im Template nutzen kann
  constructor(public auth: AuthService) {}
}
