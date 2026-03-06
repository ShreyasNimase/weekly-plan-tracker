import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { NgClass } from '@angular/common';
import { CycleService } from '../../core/services/cycle.service';
import { Cycle, CategoryBudget, CycleMember } from '../../shared/models/cycle.model';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';

export const CAT_META: Record<string, { label: string; cls: string }> = {
  Feature: { label: 'Feature', cls: 'cat-feature' },
  Bug: { label: 'Bug', cls: 'cat-bug' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'Learning', cls: 'cat-learning' },
  Other: { label: 'Other', cls: 'cat-other' },
};

export interface CategoryRow {
  category: string;
  label: string;
  cls: string;
  budgetHours: number;
  plannedHours: number;
  delta: number;   // planned - budget (negative = under)
}

export interface MemberRow {
  name: string;
  allocatedHours: number;
  isReady: boolean;
}

export interface ValidationIssue {
  text: string;
}

@Component({
  selector: 'app-freeze-review',
  standalone: true,
  imports: [
    RouterLink,
    NgClass,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSnackBarModule,
    MatDialogModule,
  ],
  templateUrl: './freeze-review.component.html',
  styleUrl: './freeze-review.component.scss',
})
export class FreezeReviewComponent implements OnInit {
  private readonly cycleService = inject(CycleService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly cycle = signal<Cycle | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly hasError = signal(false);

  // ── Derived views ──────────────────────────────────────────────────────────

  get cycleMembers(): CycleMember[] { return this.cycle()?.members ?? []; }
  get categoryBudgets(): CategoryBudget[] { return this.cycle()?.categoryBudgets ?? []; }
  get totalMembers(): number { return this.cycleMembers.length; }
  get totalBudgetHours(): number { return this.cycleMembers.reduce((s, m) => s + m.allocatedHours, 0); }
  get isFrozen(): boolean { return this.cycle()?.status === 'Frozen'; }

  /** Category summary rows — budget vs. allocated hours in this cycle */
  get categoryRows(): CategoryRow[] {
    return this.categoryBudgets.map((cb) => {
      const meta = CAT_META[cb.category] ?? { label: cb.category, cls: 'cat-other' };
      const budgetHours = +(cb.hoursBudget ?? 0);
      // We don't yet have per-assignment data from this endpoint;
      // use budgetHours as context and show 0 planned until progress API is added
      return {
        category: cb.category,
        label: meta.label,
        cls: meta.cls,
        budgetHours,
        plannedHours: 0,   // placeholder — extend when assignments progress API is wired
        delta: 0 - budgetHours,
      };
    });
  }

  /** Member summary rows */
  get memberRows(): MemberRow[] {
    return this.cycleMembers.map((cm) => ({
      name: cm.name,
      allocatedHours: cm.allocatedHours,
      isReady: false,   // backend CycleMember shape doesn't include isReady yet
    }));
  }

  /** Validation issues that block the Freeze button */
  get issues(): ValidationIssue[] {
    const list: ValidationIssue[] = [];
    for (const m of this.cycleMembers) {
      if (m.allocatedHours < 30) {
        list.push({ text: `${m.name} has ${m.allocatedHours}h allocated (needs ${30 - m.allocatedHours}h more)` });
      }
    }
    return list;
  }

  get hasIssues(): boolean { return this.issues.length > 0; }
  get canFreeze(): boolean { return !this.hasIssues && !this.isFrozen && !this.isSubmitting(); }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.cycleService.loadActive().subscribe({
      next: (cycle) => {
        if (!cycle) {
          this.snack('No active cycle found.', true);
          this.router.navigate(['/home']);
          return;
        }
        this.cycle.set(cycle);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.hasError.set(true);
        this.snack('Failed to load cycle data.', true);
      },
    });
  }

  // ── Actions ───────────────────────────────────────────────────────────────

  openFreezeDialog(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: 'Freeze the Plan?',
        message: 'Once frozen, no new items can be added. Team members can only update their progress.',
        confirmText: 'Yes, Freeze It',
        confirmColor: 'primary',
        icon: 'ac_unit',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.isSubmitting.set(true);
      this.cycleService.freezeCycle(this.cycle()!.id).subscribe({
        next: (updated) => {
          this.cycle.set(updated);
          this.isSubmitting.set(false);
          this.snackBar.open('Plan is frozen! Team can now update their progress.', 'Close', { duration: 4500 });
          this.router.navigate(['/home']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snack(err?.error?.message ?? 'Failed to freeze cycle.', true);
        },
      });
    });
  }

  openCancelDialog(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: "Cancel This Week's Planning?",
        message: "This will erase all plans and assignments for this week. Backlog items will return to available. This cannot be undone.",
        confirmText: 'Yes, Cancel Planning',
        confirmColor: 'warn',
        icon: 'warning',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.isSubmitting.set(true);
      this.cycleService.cancelCycle(this.cycle()!.id).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.snackBar.open('Planning cancelled.', 'Close', { duration: 3500 });
          this.router.navigate(['/home']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snack(err?.error?.message ?? 'Cancel failed.', true);
        },
      });
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  getStatusText(row: CategoryRow): string {
    if (row.delta === 0) return 'On track';
    return row.delta < 0 ? `Off by ${Math.abs(row.delta)}h` : `Over by ${row.delta}h`;
  }

  getStatusCls(row: CategoryRow): string {
    if (row.delta === 0) return 'status-ok';
    return row.delta < 0 ? 'status-warn' : 'status-over';
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', {
      duration: isError ? 6000 : 4000,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
