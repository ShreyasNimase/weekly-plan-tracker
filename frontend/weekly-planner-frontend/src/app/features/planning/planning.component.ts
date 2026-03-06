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
import { forkJoin, of, catchError } from 'rxjs';

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
  CLIENT_FOCUSED: { label: 'Client Focused', cls: 'cat-client' },
  TECH_DEBT: { label: 'Tech Debt', cls: 'cat-techdebt' },
  R_AND_D: { label: 'R\u0026D', cls: 'cat-rnd' },
  // legacy fallbacks
  Feature: { label: 'Client Focused', cls: 'cat-client' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'R\u0026D', cls: 'cat-rnd' },
  Bug: { label: 'Bug', cls: 'cat-techdebt' },
  Other: { label: 'Other', cls: 'cat-rnd' },
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
  get isPlanning(): boolean {
    const s = this.cycle();
    return s?.state === 'PLANNING' || s?.status === 'Planning';
  }
  get isEmpty(): boolean { return !this.isLoading() && this.assignments().length === 0; }
  get canMarkReady(): boolean {
    return this.myHours > 0 && this.isPlanning && !this.isReady();
  }

  /** 'full' | 'almost' | 'none' */
  get hoursAlertType(): 'full' | 'almost' | 'none' {
    if (this.myHours >= 30) return 'full';
    if (this.myHours >= 25) return 'almost';
    return 'none';
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

    if (!user) {
      this.isLoading.set(false);
      this.snack('No user selected.', true);
      this.router.navigate(['/home']);
      return;
    }

    // Always fetch from the API – handles direct navigation and page refresh.
    this.cycleService.loadActive().pipe(
      catchError(() => of(null))
    ).subscribe((cycle) => {
      if (!cycle || (cycle.state !== 'PLANNING' && cycle.status !== 'Planning')) {
        this.isLoading.set(false);
        this.snack('Planning is not open right now.', true);
        this.router.navigate(['/home']);
        return;
      }

      this.cycle.set(cycle);

      // ── Find the user's MemberPlan embedded in the active cycle DTO ────────
      const memberPlan = cycle.memberPlans?.find(mp => mp.memberId === user.id);
      const memberPlanId = memberPlan?.id ?? null;

      if (memberPlan) {
        this.isReady.set(memberPlan.isReady);
        this.cycleMember.set({
          id: memberPlanId!,
          teamMemberId: user.id,
          name: user.name,
          allocatedHours: 30,          // capacity is always 30h per cycle
          isReady: memberPlan.isReady,
        });
      }

      forkJoin([
        this.progressService.getCategoryProgress(cycle.id).pipe(catchError(() => of([] as CategoryProgress[]))),
        memberPlanId
          // Backend: GET /api/cycles/{id}/members/{memberId}/progress  (memberId = TeamMember.Id)
          ? this.progressService.getMemberProgress(cycle.id, user.id).pipe(catchError(() => of(null)))
          : of(null),
      ] as const).subscribe({
        next: ([catProgress, memberProgress]) => {
          // Category budget cards
          const cards: CategoryCard[] = (catProgress as CategoryProgress[]).map((cp) => {
            const meta = CAT_META[cp.category] ?? { label: cp.category, cls: 'cat-other' };
            return {
              category: cp.category, label: meta.label, cls: meta.cls,
              budgetHours: cp.budgetHours, usedHours: cp.usedHours,
              remaining: cp.remaining, utilization: cp.utilization,
            };
          });
          this.categoryCards.set(cards);

          // Assignments from member progress
          if ((memberProgress as any)?.tasks) {
            const cm = this.cycleMember();
            const assignments: Assignment[] = (memberProgress as any).tasks.map((t: any) => ({
              id: t.assignmentId,
              cycleMemberId: cm?.id ?? '',
              backlogItemId: t.backlogItemId,
              backlogItemTitle: t.title,
              backlogItemCategory: t.category,
              plannedHours: t.plannedHours,
              createdAt: '',
              progressStatus: t.progressStatus,
              hoursCompleted: t.hoursCompleted,
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
    });
  }

  // ── Change Hours ───────────────────────────────────────────────────────────
  openChangeHours(a: Assignment): void {
    const cm = this.cycleMember();
    if (!cm) {
      this.snack('Member plan not found. Please refresh.', true);
      return;
    }

    const item: BacklogItem = this.backlogItems().find((i) => i.id === a.backlogItemId)
      ?? {
        id: a.backlogItemId,
        title: a.backlogItemTitle,
        category: a.backlogItemCategory,
        status: 'IN_PLAN',
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
      // cm.id is MemberPlan.Id — markReady calls PUT /api/member-plans/{id}/ready
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
