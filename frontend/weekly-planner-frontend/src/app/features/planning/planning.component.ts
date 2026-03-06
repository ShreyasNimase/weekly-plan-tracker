import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CycleService } from '../../core/services/cycle.service';
import { AssignmentService } from '../../core/services/assignment.service';
import { AuthService } from '../../core/services/auth.service';
import { ProgressService } from '../../core/services/progress.service';
import { BacklogService } from '../../core/services/backlog.service';
import { Cycle } from '../../shared/models/cycle.model';
import { Assignment } from '../../shared/models/assignment.model';
import { CycleMemberDetail, CategoryProgress } from '../../shared/models/progress.model';
import { BacklogItem } from '../../shared/models/backlog-item.model';
import { HourCommitModalComponent, HourCommitResult } from '../../shared/components/hour-commit-modal.component';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, of, switchMap, catchError } from 'rxjs';

interface CategoryCard {
  category: string;
  label: string;
  cls: string;
  budgetHours: number;
  usedHours: number;
  remaining: number;
  utilization: number;   // 0-100
}

const CAT_META: Record<string, { label: string; cls: string }> = {
  Feature: { label: 'Feature', cls: 'cat-feature' },
  Bug: { label: 'Bug', cls: 'cat-bug' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'Learning', cls: 'cat-learning' },
  Other: { label: 'Other', cls: 'cat-other' },
};

@Component({
  selector: 'app-planning',
  standalone: true,
  imports: [
    RouterLink,
    NgClass,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
  ],
  templateUrl: './planning.component.html',
  styleUrl: './planning.component.scss',
})
export class PlanningComponent implements OnInit {
  private readonly cycleService = inject(CycleService);
  private readonly assignmentService = inject(AssignmentService);
  private readonly authService = inject(AuthService);
  private readonly progressService = inject(ProgressService);
  private readonly backlogService = inject(BacklogService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly currentUser = toSignal(this.authService.currentUser$);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly cycle = signal<Cycle | null>(null);
  readonly cycleMember = signal<CycleMemberDetail | null>(null);
  readonly assignments = signal<Assignment[]>([]);
  readonly categoryCards = signal<CategoryCard[]>([]);
  readonly backlogItems = signal<BacklogItem[]>([]);
  readonly isLoading = signal(true);
  readonly isMarkingReady = signal(false);
  readonly isReady = signal(false);
  readonly toastMsg = signal<string | null>(null);

  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Derived ───────────────────────────────────────────────────────────────
  get myHours(): number { return this.assignments().reduce((s, a) => s + a.plannedHours, 0); }
  get hoursLeft(): number { return 30 - this.myHours; }
  get myProgress(): number { return Math.min(100, Math.round((this.myHours / 30) * 100)); }
  get isPlanning(): boolean { return this.cycle()?.status === 'Planning'; }
  get canMarkReady(): boolean {
    return this.myHours > 0 && this.isPlanning && !this.isReady();
  }

  categoryCls(cat: string): string {
    return CAT_META[cat]?.cls ?? 'cat-other';
  }
  categoryLabel(cat: string): string {
    return CAT_META[cat]?.label ?? cat;
  }
  categoryBudgetLeft(cat: string): number {
    return this.categoryCards().find((c) => c.category === cat)?.remaining ?? 30;
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    const user = this.currentUser();

    this.cycleService.loadActive().pipe(
      switchMap((cycle) => {
        if (!cycle) return of([null, null, null, []] as const);
        this.cycle.set(cycle);
        return forkJoin([
          of(cycle),
          this.progressService.getCycleDetail(cycle.id).pipe(catchError(() => of(null))),
          this.progressService.getCategoryProgress(cycle.id).pipe(catchError(() => of([] as CategoryProgress[]))),
          this.backlogService.getAll({}).pipe(catchError(() => of([] as BacklogItem[]))),
        ] as const);
      })
    ).pipe(
      switchMap(([cycle, detail, catProgress, items]) => {
        if (!cycle || !detail || !user) {
          this.isLoading.set(false);
          return of(null);
        }

        // Build category cards
        const cards: CategoryCard[] = (catProgress as CategoryProgress[]).map((cp) => {
          const meta = CAT_META[cp.category] ?? { label: cp.category, cls: 'cat-other' };
          return {
            category: cp.category,
            label: meta.label,
            cls: meta.cls,
            budgetHours: cp.budgetHours,
            usedHours: cp.usedHours,
            remaining: cp.remaining,
            utilization: cp.utilization,
          };
        });
        this.categoryCards.set(cards);
        this.backlogItems.set(items as BacklogItem[]);

        // Find this user's CycleMember entry
        const cm = (detail as any).members?.find(
          (m: CycleMemberDetail) => m.teamMemberId === user.id
        ) ?? null;
        this.cycleMember.set(cm);

        if (!cm) { this.isLoading.set(false); return of(null); }

        this.isReady.set(cm.isReady);

        // Load this member's assignments via progress endpoint
        return this.progressService.getMemberProgress(cycle.id, cm.id).pipe(catchError(() => of(null)));
      })
    ).subscribe({
      next: (memberProgress: any) => {
        if (memberProgress?.tasks) {
          const cm = this.cycleMember();
          const assignments: Assignment[] = memberProgress.tasks.map((t: any) => ({
            id: t.assignmentId,
            cycleMemberId: cm?.id ?? '',
            backlogItemId: t.backlogItemId,
            backlogItemTitle: t.title,
            backlogItemCategory: t.category,
            plannedHours: t.plannedHours,
            createdAt: '',
          }));
          this.assignments.set(assignments);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack('Failed to load planning data.', true);
      },
    });
  }

  // ── Change Hours ───────────────────────────────────────────────────────────
  openChangeHours(a: Assignment): void {
    const item = this.backlogItems().find((i) => i.id === a.backlogItemId)
      ?? {
        id: a.backlogItemId,
        title: a.backlogItemTitle,
        category: a.backlogItemCategory,
        status: 'Active',
        priority: 'Medium',
        createdAt: '',
      } as BacklogItem;

    this.dialog.open(HourCommitModalComponent, {
      width: '460px',
      data: {
        item,
        cycleMemberId: this.cycleMember()!.id,
        myHoursLeft: this.hoursLeft + a.plannedHours, // add back existing hours
        categoryBudgetLeft: this.categoryBudgetLeft(a.backlogItemCategory) + a.plannedHours,
        existingAssignmentId: a.id,
        existingHours: a.plannedHours,
      },
    }).afterClosed().subscribe((res: HourCommitResult | { error: string } | null) => {
      if (!res) return;
      if ('error' in res) { this.snack(res.error, true); return; }
      this.assignments.update((list) =>
        list.map((x) => x.id === a.id ? { ...x, plannedHours: res.hours } : x)
      );
      this.showToast(`Updated: ${a.backlogItemTitle}`);
    });
  }

  // ── Remove ────────────────────────────────────────────────────────────────
  removeAssignment(a: Assignment): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Remove this item?',
        message: `This will remove "${a.backlogItemTitle}" from your plan. The hours will be returned to the category budget.`,
        confirmText: 'Yes, Remove',
        confirmColor: 'warn',
        icon: 'delete',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      this.assignmentService.remove(a.id).subscribe({
        next: () => {
          this.assignments.update((l) => l.filter((x) => x.id !== a.id));
          this.load(); // refresh category cards
          this.snack(`"${a.backlogItemTitle}" removed.`);
        },
        error: (err) => this.snack(err?.error?.message ?? 'Remove failed.', true),
      });
    });
  }

  // ── Mark Ready ────────────────────────────────────────────────────────────
  openMarkReady(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Mark Your Plan as Ready?',
        message: "Once marked ready, your plan will be visible to the Team Lead for review. You can still make changes until the plan is frozen.",
        confirmText: "Yes, I'm Done",
        confirmColor: 'primary',
        icon: 'check_circle',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      const cm = this.cycleMember();
      if (!cm) return;
      this.isMarkingReady.set(true);
      this.assignmentService.markReady(cm.id).subscribe({
        next: () => {
          this.isMarkingReady.set(false);
          this.isReady.set(true);
          this.snack('Your plan is marked as ready! 🎉');
        },
        error: (err) => {
          this.isMarkingReady.set(false);
          this.snack(err?.error?.message ?? 'Failed to mark ready.', true);
        },
      });
    });
  }

  // ── Navigate to pick ──────────────────────────────────────────────────────
  goToPick(): void { this.router.navigate(['/planning', 'pick']); }

  // ── Toast ─────────────────────────────────────────────────────────────────
  showToast(msg: string): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastMsg.set(msg);
    this.toastTimer = setTimeout(() => this.toastMsg.set(null), 3000);
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', {
      duration: isError ? 6000 : 3500,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
