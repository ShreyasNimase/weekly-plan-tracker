import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
    title: string;
    message: string;
    confirmText: string;
    confirmColor?: 'primary' | 'warn' | 'accent';
    icon?: string;
}

@Component({
    selector: 'app-confirm-dialog',
    standalone: true,
    imports: [MatDialogModule, MatButtonModule, MatIconModule],
    template: `
    <div class="confirm-dialog">
      <div class="confirm-header">
        <mat-icon class="confirm-icon" [class]="'icon-' + (data.confirmColor ?? 'primary')">
          {{ data.icon ?? 'help_outline' }}
        </mat-icon>
        <h2 mat-dialog-title>{{ data.title }}</h2>
      </div>
      <mat-dialog-content>
        <p class="confirm-message">{{ data.message }}</p>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-stroked-button [mat-dialog-close]="false">Cancel</button>
        <button mat-flat-button
                [color]="data.confirmColor ?? 'primary'"
                [mat-dialog-close]="true"
                class="confirm-btn">
          {{ data.confirmText }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
    styles: [`
    .confirm-dialog { padding: 8px; min-width: 320px; }
    .confirm-header { display: flex; align-items: center; gap: 12px; padding: 8px 0 0; }
    h2 { margin: 0; font-size: 1.05rem; font-weight: 700; }
    .confirm-icon { font-size: 28px; width: 28px; height: 28px; }
    .icon-primary { color: #1565c0; }
    .icon-warn    { color: #c62828; }
    .icon-accent  { color: #6a1b9a; }
    .confirm-message { font-size: 0.93rem; line-height: 1.6; margin: 8px 0 0; color: #555; }
    mat-dialog-actions { padding-top: 16px; gap: 8px; }
    .confirm-btn { border-radius: 8px !important; }
  `],
})
export class ConfirmDialogComponent {
    constructor(
        @Inject(MAT_DIALOG_DATA) public data: ConfirmDialogData,
        public dialogRef: MatDialogRef<ConfirmDialogComponent>
    ) { }
}
