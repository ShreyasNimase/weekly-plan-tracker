import { Component, inject, signal, computed } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NgClass } from '@angular/common';
import { AssignmentService } from '../../core/services/assignment.service';
import { BacklogItem } from '../models/backlog-item.model';
import { Assignment } from '../models/assignment.model';

// ── Custom 0.5-step validator ────────────────────────────────────────────────
function halfHourStep(ctrl: AbstractControl): ValidationErrors | null {
  const v = ctrl.value;
  if (v === null || v === undefined || v === '') return null;
  return +v % 0.5 === 0 ? null : { halfStep: true };
}

export interface HourCommitData {
  item: BacklogItem;
  cycleMemberId: string;     // CycleMember PK (for POST)
  myHoursLeft: number;     // 30 - already planned
  categoryBudgetLeft: number;     // category remaining hours
  existingAssignmentId?: string;  // present → PUT (change hours)
  existingHours?: number;     // pre-fill in change-hours mode
}

export interface HourCommitResult {
  hours: number;
  assignmentId: string;
  backlogItemTitle: string;
  backlogItemCategory: string;
  backlogItemId: string;
}

const CAT_META: Record<string, { label: string }> = {
  Feature: { label: 'Feature' },
  Bug: { label: 'Bug' },
  TechDebt: { label: 'Tech Debt' },
  Learning: { label: 'Learning' },
  Other: { label: 'Other' },
};

@Component({
  selector: 'app-hour-commit-modal',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
    NgClass,
  ],
  template: `
<div class="hcm-dialog">

  <!-- Header -->
  <div class="hcm-header">
    <mat-icon class="hcm-icon">schedule</mat-icon>
    <h2 mat-dialog-title>How many hours will you work on this?</h2>
  </div>

  <!-- Item context -->
  <mat-dialog-content>
    <div class="item-context">
      <span class="item-title">{{ data.item.title }}</span>
      <span class="cat-pill" [ngClass]="catCls">{{ catLabel }}</span>
    </div>

    <div class="context-hints">
      <span>Your hours left: <strong>{{ data.myHoursLeft }}h</strong></span>
      <span>{{ catLabel }} budget left: <strong>{{ data.categoryBudgetLeft }}h</strong></span>
      @if (data.item.estimatedHours) {
        <span class="hint-est">Estimate for this item: <strong>{{ data.item.estimatedHours }}h</strong></span>
      }
    </div>

    <!-- Hours input -->
    <form [formGroup]="form" class="hcm-form">
      <mat-form-field appearance="outline" class="hours-field">
        <mat-label>Hours to commit</mat-label>
        <mat-icon matPrefix>schedule</mat-icon>
        <input matInput type="number" formControlName="hours"
               min="0.5" step="0.5" placeholder="e.g. 4" />
        @if (form.controls.hours.hasError('required') && form.controls.hours.touched) {
          <mat-error>Please enter hours.</mat-error>
        }
        @if (form.controls.hours.hasError('min') && form.controls.hours.touched) {
          <mat-error>Minimum is 0.5h.</mat-error>
        }
        @if (form.controls.hours.hasError('halfStep') && form.controls.hours.touched) {
          <mat-error>Must be in 0.5h increments (e.g. 1, 1.5, 2).</mat-error>
        }
        @if (exceeds30()) {
          <mat-error>You only have {{ data.myHoursLeft }}h left to plan.</mat-error>
        }
      </mat-form-field>
    </form>

    <!-- Category budget warning (soft, non-blocking) -->
    @if (exceedsCatBudget() && !exceeds30()) {
      <div class="warn-banner">
        <mat-icon>warning</mat-icon>
        ⚠ This exceeds the {{ catLabel }} budget by {{ overCatBy() }}h.
      </div>
    }
  </mat-dialog-content>

  <!-- Actions -->
  <mat-dialog-actions align="end">
    <button mat-button (click)="cancel()" [disabled]="isSaving()">Cancel</button>
    <button mat-flat-button color="primary"
            [disabled]="form.invalid || exceeds30() || isSaving()"
            (click)="confirm()">
      @if (isSaving()) { <mat-spinner diameter="16"></mat-spinner> Saving… }
      @else { <mat-icon>{{ isEdit ? 'save' : 'add_task' }}</mat-icon> {{ isEdit ? 'Save Changes' : 'Add to My Plan' }} }
    </button>
  </mat-dialog-actions>
</div>
  `,
  styles: [`
.hcm-dialog { min-width: 360px; max-width: 480px; }

.hcm-header {
  display: flex; align-items: center; gap: 10px; padding: 20px 24px 0;
  .hcm-icon { color: var(--brand-primary); font-size: 26px; width: 26px; height: 26px; }
  h2 { font-size: 1.05rem; font-weight: 700; margin: 0; }
}

mat-dialog-content { padding: 16px 24px !important; }

.item-context {
  display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
  margin-bottom: 12px;
}

.item-title { font-weight: 700; font-size: 0.92rem; }

.cat-pill {
  font-size: 0.7rem; font-weight: 700; padding: 2px 10px;
  border-radius: 12px; color: #fff;
  &.cat-feature  { background: #3b82f6; }
  &.cat-bug      { background: #ef4444; }
  &.cat-techdebt { background: #f97316; }
  &.cat-learning { background: #22c55e; }
  &.cat-other    { background: #8b5cf6; }
}

.context-hints {
  display: flex; gap: 16px; flex-wrap: wrap;
  font-size: 0.83rem; color: var(--text-secondary); margin-bottom: 16px;
  strong { color: var(--text-primary); }
}

.hint-est { font-style: italic; }

.hcm-form { display: block; }
.hours-field { width: 100%; }

.warn-banner {
  display: flex; align-items: center; gap: 6px;
  background: #fff8e1; color: #e65100;
  border-radius: 8px; padding: 10px 14px;
  font-size: 0.83rem; font-weight: 600;
  margin-top: 8px;
  mat-icon { font-size: 16px; width: 16px; height: 16px; }
}

body.dark-theme .warn-banner { background: rgba(230,81,0,0.12); color: #ffcc80; }

mat-dialog-actions { padding: 8px 24px 20px !important; gap: 8px; }
mat-spinner { display: inline-block; margin-right: 4px; }
  `],
})
export class HourCommitModalComponent {
  readonly data = inject<HourCommitData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<HourCommitModalComponent>);
  private readonly assignmentService = inject(AssignmentService);
  private readonly fb = inject(FormBuilder);

  readonly isSaving = signal(false);
  readonly isEdit = !!this.data.existingAssignmentId;

  readonly form = this.fb.group({
    hours: [
      this.data.existingHours ?? Math.min(this.data.item.estimatedHours ?? 4, this.data.myHoursLeft),
      [Validators.required, Validators.min(0.5), halfHourStep],
    ],
  });

  get catCls(): string { return 'cat-' + (this.data.item.category ?? 'other').toLowerCase().replace(/\s+/g, ''); }
  get catLabel(): string { return CAT_META[this.data.item.category]?.label ?? this.data.item.category; }
  get inputHours(): number { return +(this.form.value.hours ?? 0); }

  exceeds30(): boolean { return this.inputHours > this.data.myHoursLeft; }
  exceedsCatBudget(): boolean { return this.inputHours > this.data.categoryBudgetLeft; }
  overCatBy(): number { return +(this.inputHours - this.data.categoryBudgetLeft).toFixed(1); }

  confirm(): void {
    if (this.form.invalid || this.exceeds30()) return;
    const hours = this.inputHours;
    this.isSaving.set(true);

    const call$: import('rxjs').Observable<Assignment> = this.isEdit
      ? this.assignmentService.updateHours(this.data.existingAssignmentId!, { plannedHours: hours })
      : this.assignmentService.claim({
        cycleMemberId: this.data.cycleMemberId,
        backlogItemId: this.data.item.id,
        plannedHours: hours,
      });

    call$.subscribe({
      next: (result: Assignment) => {
        this.isSaving.set(false);
        const res: HourCommitResult = {
          hours,
          assignmentId: (result as any).id,
          backlogItemTitle: this.data.item.title,
          backlogItemCategory: this.data.item.category,
          backlogItemId: this.data.item.id,
        };
        this.dialogRef.close(res);
      },
      error: (err) => {
        this.isSaving.set(false);
        // Re-throw so caller can handle via snackbar
        this.dialogRef.close({ error: err?.error?.message ?? 'Save failed.' });
      },
    });
  }

  cancel(): void { this.dialogRef.close(null); }
}
