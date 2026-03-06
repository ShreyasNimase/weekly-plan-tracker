import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgClass } from '@angular/common';
import { AssignmentService } from '../../core/services/assignment.service';
import { Assignment } from '../models/assignment.model';

export interface ProgressUpdateData {
  assignment: Assignment;
  plannedHours: number;
}

const STATUS_OPTIONS = [
  { value: 'NOT_STARTED', label: 'Not Started' },
  { value: 'IN_PROGRESS', label: 'In Progress' },
  { value: 'COMPLETED', label: 'Completed' },
  { value: 'BLOCKED', label: 'Blocked' },
];

const CAT_META: Record<string, { label: string; cls: string }> = {
  Feature: { label: 'Feature', cls: 'cat-feature' },
  Bug: { label: 'Bug', cls: 'cat-bug' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'Learning', cls: 'cat-learning' },
  Other: { label: 'Other', cls: 'cat-other' },
};

@Component({
  selector: 'app-progress-update-modal',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    NgClass,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  template: `
<div class="pum-dialog">
  <div class="pum-header">
    <mat-icon class="pum-icon">track_changes</mat-icon>
    <h2 mat-dialog-title>Update Progress</h2>
  </div>

  <mat-dialog-content>
    <!-- Task context -->
    <div class="task-context">
      <span class="task-title">{{ data.assignment.backlogItemTitle }}</span>
      <span class="cat-pill" [ngClass]="catCls">{{ catLabel }}</span>
      <span class="planned-lbl">Planned: {{ data.plannedHours }}h</span>
    </div>

    <form [formGroup]="form" class="pum-form">
      <!-- Status -->
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Status</mat-label>
        <mat-select formControlName="status" (selectionChange)="onStatusChange()">
          @for (opt of statusOptions; track opt.value) {
            <mat-option [value]="opt.value">{{ opt.label }}</mat-option>
          }
        </mat-select>
        @if (form.controls.status.hasError('required') && form.controls.status.touched) {
          <mat-error>Status is required.</mat-error>
        }
      </mat-form-field>

      <!-- Hours completed -->
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Hours completed so far</mat-label>
        <mat-icon matPrefix>schedule</mat-icon>
        <input matInput type="number" formControlName="hoursCompleted"
               min="0" [max]="data.plannedHours" step="0.5" />
        @if (form.controls.hoursCompleted.hasError('required') && form.controls.hoursCompleted.touched) {
          <mat-error>Required.</mat-error>
        }
        @if (form.controls.hoursCompleted.hasError('max') && form.controls.hoursCompleted.touched) {
          <mat-error>Cannot exceed planned {{ data.plannedHours }}h.</mat-error>
        }
        @if (form.controls.hoursCompleted.hasError('min') && form.controls.hoursCompleted.touched) {
          <mat-error>Must be 0 or more.</mat-error>
        }
      </mat-form-field>

      <!-- Notes -->
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Notes (optional)</mat-label>
        <textarea matInput formControlName="notes" rows="3"
                  placeholder="Any blockers or notes? (optional)"></textarea>
      </mat-form-field>
    </form>
  </mat-dialog-content>

  <mat-dialog-actions align="end">
    <button mat-button (click)="cancel()" [disabled]="isSaving()">Cancel</button>
    <button mat-flat-button color="primary"
            [disabled]="form.invalid || isSaving()"
            (click)="save()">
      @if (isSaving()) { <mat-spinner diameter="16"></mat-spinner> Saving… }
      @else { <mat-icon>save</mat-icon> Save Update }
    </button>
  </mat-dialog-actions>
</div>
  `,
  styles: [`
.pum-dialog { min-width: 360px; max-width: 480px; }
.pum-header { display: flex; align-items: center; gap: 10px; padding: 20px 24px 0;
  .pum-icon { color: #3b82f6; font-size: 24px; width: 24px; height: 24px; }
  h2 { font-size: 1.05rem; font-weight: 700; margin: 0; } }
mat-dialog-content { padding: 16px 24px !important; }
.task-context { display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
  margin-bottom: 16px; }
.task-title { font-weight: 700; font-size: 0.92rem; flex: 1; min-width: 120px; }
.cat-pill { font-size: 0.7rem; font-weight: 700; padding: 2px 10px; border-radius: 12px; color: #fff;
  &.cat-feature { background:#3b82f6; } &.cat-bug { background:#ef4444; }
  &.cat-techdebt { background:#f97316; } &.cat-learning { background:#22c55e; }
  &.cat-other { background:#8b5cf6; } }
.planned-lbl { font-size: 0.8rem; color: var(--text-secondary); }
.pum-form { display: flex; flex-direction: column; gap: 4px; }
.full-width { width: 100%; }
mat-dialog-actions { padding: 8px 24px 20px !important; gap: 8px; }
mat-spinner { display: inline-block; margin-right: 4px; }
  `],
})
export class ProgressUpdateModalComponent {
  readonly data = inject<ProgressUpdateData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ProgressUpdateModalComponent>);
  private readonly assignmentService = inject(AssignmentService);
  private readonly fb = inject(FormBuilder);

  readonly statusOptions = STATUS_OPTIONS;
  readonly isSaving = signal(false);

  readonly form = this.fb.group({
    status: [this.data.assignment.progressStatus ?? 'NOT_STARTED', Validators.required],
    hoursCompleted: [
      this.data.assignment.hoursCompleted ?? 0,
      [Validators.required, Validators.min(0), Validators.max(this.data.plannedHours)],
    ],
    notes: [this.data.assignment.notes ?? ''],
  });

  get catCls(): string { return CAT_META[this.data.assignment.backlogItemCategory]?.cls ?? 'cat-other'; }
  get catLabel(): string { return CAT_META[this.data.assignment.backlogItemCategory]?.label ?? this.data.assignment.backlogItemCategory; }

  onStatusChange(): void {
    const status = this.form.value.status;
    if (status === 'COMPLETED') {
      this.form.controls.hoursCompleted.setValue(this.data.plannedHours);
      this.form.controls.hoursCompleted.disable();
    } else if (status === 'NOT_STARTED') {
      this.form.controls.hoursCompleted.setValue(0);
      this.form.controls.hoursCompleted.disable();
    } else {
      this.form.controls.hoursCompleted.enable();
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.isSaving.set(true);
    const raw = this.form.getRawValue();

    this.assignmentService.updateProgress(this.data.assignment.id, {
      progressStatus: raw.status!,
      hoursCompleted: raw.hoursCompleted!,
      notes: raw.notes ?? '',
    }).subscribe({
      next: (updated) => {
        this.isSaving.set(false);
        this.dialogRef.close(updated);
      },
      error: (err: any) => {
        this.isSaving.set(false);
        this.dialogRef.close({ error: err?.error?.message ?? 'Update failed.' });
      },
    });
  }

  cancel(): void { this.dialogRef.close(null); }
}
