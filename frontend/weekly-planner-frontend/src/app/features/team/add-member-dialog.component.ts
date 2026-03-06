import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TeamService } from '../../core/services/team.service';
import { signal } from '@angular/core';

@Component({
    selector: 'app-add-member-dialog',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatCheckboxModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
    ],
    template: `
    <div class="dialog-header">
      <mat-icon class="dialog-icon">person_add</mat-icon>
      <h2 mat-dialog-title>Add Team Member</h2>
    </div>

    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Member Name</mat-label>
          <mat-icon matPrefix>badge</mat-icon>
          <input matInput formControlName="name" placeholder="e.g. Jordan Lee" autocomplete="off" />
          @if (form.controls.name.hasError('required')) {
            <mat-error>Name is required</mat-error>
          }
          @if (form.controls.name.hasError('minlength')) {
            <mat-error>Name must be at least 2 characters</mat-error>
          }
        </mat-form-field>

        <mat-checkbox formControlName="isLead" class="lead-check">
          <span class="lead-label">
            <mat-icon class="star-icon">star</mat-icon>
            Make this member the Team Lead
          </span>
        </mat-checkbox>
        <p class="lead-note text-muted">Only one lead is allowed. Assigning a new lead will remove the current one.</p>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button
        mat-flat-button
        class="save-btn"
        [disabled]="form.invalid || isLoading()"
        (click)="onSave()"
      >
        @if (isLoading()) {
          <mat-spinner diameter="18"></mat-spinner>
        } @else {
          <mat-icon>check</mat-icon>
        }
        Add Member
      </button>
    </mat-dialog-actions>
  `,
    styles: [`
    .dialog-header {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 20px 24px 0;

      .dialog-icon { color: var(--brand-primary); font-size: 28px; width: 28px; height: 28px; }
      h2 { margin: 0; font-size: 1.2rem; font-weight: 700; }
    }
    mat-dialog-content { padding: 16px 24px !important; }
    .dialog-form { display: flex; flex-direction: column; gap: 16px; padding-top: 8px; }
    .lead-check { font-size: 0.9rem; }
    .lead-label { display: flex; align-items: center; gap: 4px; font-weight: 500; }
    .star-icon { font-size: 16px; width: 16px; height: 16px; color: #f9a825; }
    .lead-note { font-size: 0.78rem; margin: -8px 0 0; }
    .save-btn {
      background: linear-gradient(135deg, #1565c0, #42a5f5) !important;
      color: white !important;
      border-radius: 8px !important;
      display: flex; align-items: center; gap: 6px;
    }
  `],
})
export class AddMemberDialogComponent {
    private readonly teamService = inject(TeamService);
    private readonly dialogRef = inject(MatDialogRef<AddMemberDialogComponent>);
    private readonly snackBar = inject(MatSnackBar);
    private readonly fb = inject(FormBuilder);

    readonly isLoading = signal(false);

    readonly form = this.fb.group({
        name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
        isLead: [false],
    });

    onSave(): void {
        if (this.form.invalid) return;
        this.isLoading.set(true);

        this.teamService.create({
            name: this.form.value.name!,
            isLead: this.form.value.isLead!,
        }).subscribe({
            next: (member) => {
                this.snackBar.open(`${member.name} added successfully!`, 'Close', { duration: 3000 });
                this.dialogRef.close(true);
            },
            error: (err) => {
                this.isLoading.set(false);
                const msg = err?.error?.message ?? 'Failed to add member';
                this.snackBar.open(msg, 'Dismiss', { duration: 5000 });
            },
        });
    }
}
