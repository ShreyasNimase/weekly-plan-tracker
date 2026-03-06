import { Component, inject, signal, Inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AssignmentService } from '../../core/services/assignment.service';
import { Assignment } from '../../shared/models/assignment.model';
import { BacklogItem } from '../../shared/models/backlog-item.model';

export interface ClaimDialogData {
  cycleMemberId: string;  // This is CycleMember.Id (PK of the junction table)
  cycleId: string;
  remainingHours: number;
  existingAssignments: Assignment[];
  backlogItems: BacklogItem[];
}

@Component({
  selector: 'app-claim-item-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  template: `
    <div class="dialog-header">
      <mat-icon class="dialog-icon">add_task</mat-icon>
      <h2 mat-dialog-title>Claim a Task</h2>
    </div>

    <mat-dialog-content>
      <p class="remaining-info">
        <mat-icon class="info-icon">schedule</mat-icon>
        You have <strong>{{ data.remainingHours }}h</strong> remaining of your 30h plan
      </p>

      <form [formGroup]="form" class="claim-form">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Backlog Item</mat-label>
          <mat-icon matPrefix>list_alt</mat-icon>
          <mat-select formControlName="backlogItemId">
            @if (availableItems.length === 0) {
              <mat-option disabled>No available items</mat-option>
            }
            @for (item of availableItems; track item.id) {
              <mat-option [value]="item.id">
                <span class="option-category">[{{ item.category }}]</span>
                {{ item.title }}
                @if (item.estimatedHours) {
                  <span class="option-hrs">· {{ item.estimatedHours }}h est.</span>
                }
              </mat-option>
            }
          </mat-select>
          @if (form.controls.backlogItemId.hasError('required')) {
            <mat-error>Please select an item</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Planned Hours</mat-label>
          <mat-icon matPrefix>schedule</mat-icon>
          <input matInput type="number" formControlName="plannedHours"
                 min="0.5" [max]="data.remainingHours" step="0.5"
                 placeholder="e.g. 2.5" />
          <mat-hint>0.5h increments · max {{ data.remainingHours }}h remaining</mat-hint>
          @if (form.controls.plannedHours.hasError('required')) {
            <mat-error>Hours are required</mat-error>
          }
          @if (form.controls.plannedHours.hasError('min')) {
            <mat-error>Minimum 0.5h</mat-error>
          }
          @if (form.controls.plannedHours.hasError('max')) {
            <mat-error>Exceeds your remaining {{ data.remainingHours }}h</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button class="claim-btn"
              [disabled]="form.invalid || isLoading()"
              (click)="onClaim()">
        @if (isLoading()) {<mat-spinner diameter="18"></mat-spinner>}
        @else {<mat-icon>add_task</mat-icon>}
        Claim Task
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-header {
      display: flex; align-items: center; gap: 10px;
      padding: 20px 24px 0;
      .dialog-icon { color: var(--brand-primary); font-size: 28px; width: 28px; height: 28px; }
      h2 { margin: 0; font-size: 1.2rem; font-weight: 700; }
    }
    mat-dialog-content { padding: 16px 24px !important; }
    .remaining-info {
      display: flex; align-items: center; gap: 6px;
      font-size: 0.88rem; color: var(--text-secondary);
      margin-bottom: 16px;
      .info-icon { font-size: 16px; width: 16px; height: 16px; }
    }
    .claim-form { display: flex; flex-direction: column; gap: 16px; }
    .option-category { color: var(--brand-primary); font-weight: 600; margin-right: 4px; }
    .option-hrs { color: var(--text-secondary); font-size: 0.82rem; }
    .claim-btn {
      background: linear-gradient(135deg, #1565c0, #42a5f5) !important;
      color: white !important; border-radius: 8px !important;
      display: flex; align-items: center; gap: 6px;
    }
  `],
})
export class ClaimItemDialogComponent {
  readonly data = inject<ClaimDialogData>(MAT_DIALOG_DATA);
  private readonly assignmentService = inject(AssignmentService);
  private readonly dialogRef = inject(MatDialogRef<ClaimItemDialogComponent>);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly isLoading = signal(false);

  readonly form = this.fb.group({
    backlogItemId: ['', Validators.required],
    plannedHours: [
      null as number | null,
      [Validators.required, Validators.min(0.5), Validators.max(this.data.remainingHours)],
    ],
  });

  /** Filter out already-claimed items */
  get availableItems(): BacklogItem[] {
    const claimedIds = new Set(this.data.existingAssignments.map(a => a.backlogItemId));
    return this.data.backlogItems.filter(
      item => !claimedIds.has(item.id) && item.status?.toUpperCase() !== 'ARCHIVED'
    );
  }

  onClaim(): void {
    if (this.form.invalid) return;
    const hours = this.form.value.plannedHours!;

    if (hours % 0.5 !== 0) {
      this.snackBar.open('Hours must be in 0.5 increments (e.g. 1.5, 2.0)', 'Dismiss', { duration: 5000 });
      return;
    }

    this.isLoading.set(true);
    this.assignmentService.claim({
      memberPlanId: this.data.cycleMemberId,  // data.cycleMemberId holds the MemberPlan.Id
      backlogItemId: this.form.value.backlogItemId!,
      committedHours: hours,
    }).subscribe({
      next: (assignment) => {
        this.snackBar.open('Task claimed!', 'Close', { duration: 3000 });
        this.dialogRef.close(assignment);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err?.error?.message ?? 'Failed to claim task.';
        this.snackBar.open(msg, 'Dismiss', { duration: 6000 });
      },
    });
  }
}
