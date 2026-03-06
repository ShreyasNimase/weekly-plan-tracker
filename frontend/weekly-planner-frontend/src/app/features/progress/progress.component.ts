import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CycleService } from '../../core/services/cycle.service';
import { ProgressService } from '../../core/services/progress.service';
import { AuthService } from '../../core/services/auth.service';
import { Cycle } from '../../shared/models/cycle.model';
import { CycleProgress, MemberProgressSummary, MemberProgress, MemberTask } from '../../shared/models/progress.model';
import { Assignment } from '../../shared/models/assignment.model';
import { ProgressUpdateModalComponent } from '../../shared/components/progress-update-modal.component';
import { toSignal } from '@angular/core/rxjs-interop';
import { switchMap, forkJoin, of, catchError } from 'rxjs';

const STATUS_CLS: Record<string, string> = {
  NOT_STARTED: 'badge-not-started',
  IN_PROGRESS: 'badge-in-progress',
  COMPLETED: 'badge-completed',
  BLOCKED: 'badge-blocked',
};
const STATUS_LABEL: Record<string, string> = {
  NOT_STARTED: 'Not Started',
  IN_PROGRESS: 'In Progress',
  COMPLETED: 'Completed',
  BLOCKED: 'Blocked',
};

@Component({
  selector: 'app-progress',
  standalone: true,
  imports: [
    RouterLink, DatePipe, DecimalPipe, NgClass,
    MatButtonModule, MatIconModule, MatProgressBarModule,
    MatProgressSpinnerModule, MatDividerModule,
    MatExpansionModule, MatSnackBarModule, MatDialogModule,
  ],
  templateUrl: './progress.component.html',
  styleUrl: './progress.component.scss',
})
export class ProgressComponent implements OnInit {
  private readonly cycleService = inject(CycleService);
  private readonly progressService = inject(ProgressService);
  private readonly authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly currentUser = toSignal(this.authService.currentUser$);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly cycle = signal<Cycle | null>(null);
  readonly myTasks = signal<MemberTask[]>([]);
  readonly cycleProgress = signal<CycleProgress | null>(null);
  readonly isLoading = signal(true);
  readonly toastMsg = signal<string | null>(null);
  readonly expandedMember = signal<string | null>(null);
  readonly memberDetails = signal<Record<string, MemberProgress>>({});
  readonly loadingMember = signal<string | null>(null);

  private cycleMemberId = '';
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Derived ───────────────────────────────────────────────────────────────
  get isLead(): boolean { return !!this.currentUser()?.isLead; }
  get planningDate(): string { return this.cycle()?.weekStartDate ?? ''; }

  get myCompletedHours(): number {
    return this.myTasks().reduce((s, t) => s + ((t as any)['hoursCompleted'] ?? 0), 0);
  }
  get myPlannedHours(): number {
    return this.myTasks().reduce((s, t) => s + t.plannedHours, 0);
  }
  get myProgressPct(): number {
    return this.myPlannedHours ? Math.min(100, Math.round((this.myCompletedHours / this.myPlannedHours) * 100)) : 0;
  }

  statusCls(s: string): string { return STATUS_CLS[s] ?? 'badge-not-started'; }
  statusLabel(s: string): string { return STATUS_LABEL[s] ?? s; }

  /** Safe accessor for extra progress fields that MemberTask doesn't type */
  taskStatus(t: any): string { return t['progressStatus'] ?? 'NOT_STARTED'; }
  taskHoursCompleted(t: any): number { return +(t['hoursCompleted'] ?? 0); }
  taskNotes(t: any): string { return t['notes'] ?? ''; }

  /** Tasks sorted: BLOCKED first, then IN_PROGRESS, NOT_STARTED, COMPLETED */
  get sortedMyTasks(): any[] {
    const order: Record<string, number> = { BLOCKED: 0, IN_PROGRESS: 1, NOT_STARTED: 2, COMPLETED: 3 };
    return [...this.myTasks()].sort((a, b) =>
      (order[this.taskStatus(a)] ?? 4) - (order[this.taskStatus(b)] ?? 4)
    );
  }

  catCls(cat: string): string {
    const m: Record<string, string> = {
      Feature: 'cat-feature', Bug: 'cat-bug', TechDebt: 'cat-techdebt',
      Learning: 'cat-learning', Other: 'cat-other',
    };
    return m[cat] ?? 'cat-other';
  }
  catLabel(cat: string): string {
    const m: Record<string, string> = {
      Feature: 'Feature', Bug: 'Bug', TechDebt: 'Tech Debt',
      Learning: 'Learning', Other: 'Other',
    };
    return m[cat] ?? cat;
  }

  teamMembers(): MemberProgressSummary[] { return this.cycleProgress()?.members ?? []; }

  memberBlockedCount(m: MemberProgressSummary): number {
    return (this.memberDetails()[m.cycleMemberId]?.tasks ?? []).filter(
      (t: any) => t.progressStatus === 'BLOCKED'
    ).length;
  }
  memberCompletedCount(m: MemberProgressSummary): number {
    return (this.memberDetails()[m.cycleMemberId]?.tasks ?? []).filter(
      (t: any) => t.progressStatus === 'COMPLETED'
    ).length;
  }
  memberTasks(m: MemberProgressSummary): any[] {
    return this.memberDetails()[m.cycleMemberId]?.tasks ?? [];
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    // State guard: progress requires an active cycle in FROZEN state
    const cached = this.cycleService.activeCycle;
    if (!cached || (!['FROZEN', 'Frozen'].includes(cached.state ?? cached.status ?? ''))) {
      this.snack('No frozen cycle active. Freeze the plan first.', true);
      this.router.navigate(['/home']);
      return;
    }
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    const user = this.currentUser();

    this.cycleService.loadActive().pipe(
      switchMap((cycle) => {
        if (!cycle) { this.isLoading.set(false); return of(null); }
        this.cycle.set(cycle);
        return forkJoin([
          this.progressService.getCycleProgress(cycle.id).pipe(catchError(() => of(null))),
          this.progressService.getCycleDetail(cycle.id).pipe(catchError(() => of(null))),
        ] as const);
      }),
      switchMap((result) => {
        if (!result) return of(null);
        const [cycleProgress, detail] = result;
        if (cycleProgress) this.cycleProgress.set(cycleProgress as CycleProgress);

        const user = this.currentUser();
        if (!user || !detail) { this.isLoading.set(false); return of(null); }

        // Find cycleMember for this user
        const cm = (detail as any).members?.find((m: any) => m.teamMemberId === user.id);
        if (!cm) { this.isLoading.set(false); return of(null); }
        this.cycleMemberId = cm.id;

        const cycle = this.cycle()!;
        return this.progressService.getMemberProgress(cycle.id, cm.id).pipe(catchError(() => of(null)));
      })
    ).subscribe({
      next: (mp: any) => {
        if (mp?.tasks) this.myTasks.set(mp.tasks);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack('Failed to load progress data.', true);
      },
    });
  }

  // ── Update modal ──────────────────────────────────────────────────────────
  openUpdate(task: MemberTask): void {
    const a: Assignment = {
      id: task.assignmentId,
      cycleMemberId: this.cycleMemberId,
      backlogItemId: task.backlogItemId,
      backlogItemTitle: task.title,
      backlogItemCategory: task.category,
      plannedHours: task.plannedHours,
      createdAt: '',
      progressStatus: (task as any).progressStatus ?? 'NOT_STARTED',
      hoursCompleted: (task as any).hoursCompleted ?? 0,
      notes: (task as any).notes ?? '',
    };

    this.dialog.open(ProgressUpdateModalComponent, {
      width: '460px',
      data: { assignment: a, plannedHours: task.plannedHours },
    }).afterClosed().subscribe((res: any) => {
      if (!res) return;
      if (res.error) { this.snack(res.error, true); return; }
      // Patch task in myTasks list
      this.myTasks.update((list) =>
        list.map((t) => t.assignmentId === task.assignmentId
          ? { ...t, progressStatus: res.progressStatus, hoursCompleted: res.hoursCompleted, notes: res.notes } as any
          : t
        )
      );
      this.showToast(`Progress updated: ${task.title} → ${this.statusLabel(res.progressStatus)}`);
    });
  }

  // ── Team overview (Lead): toggle member detail ────────────────────────────
  toggleMemberDetail(m: MemberProgressSummary): void {
    if (this.expandedMember() === m.cycleMemberId) {
      this.expandedMember.set(null);
      return;
    }
    this.expandedMember.set(m.cycleMemberId);
    if (this.memberDetails()[m.cycleMemberId]) return; // already loaded

    this.loadingMember.set(m.cycleMemberId);
    const cycle = this.cycle();
    if (!cycle) return;

    this.progressService.getMemberProgress(cycle.id, m.cycleMemberId).subscribe({
      next: (mp) => {
        this.memberDetails.update((d) => ({ ...d, [m.cycleMemberId]: mp }));
        this.loadingMember.set(null);
      },
      error: () => this.loadingMember.set(null),
    });
  }

  isMemberExpanded(m: MemberProgressSummary): boolean {
    return this.expandedMember() === m.cycleMemberId;
  }

  isLoadingMember(m: MemberProgressSummary): boolean {
    return this.loadingMember() === m.cycleMemberId;
  }

  // ── Toast ─────────────────────────────────────────────────────────────────
  showToast(msg: string): void {
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastMsg.set(msg);
    this.toastTimer = setTimeout(() => this.toastMsg.set(null), 3000);
  }

  isCurrentUser(m: MemberProgressSummary): boolean {
    return m.teamMemberId === this.currentUser()?.id;
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', { duration: isError ? 6000 : 3500 });
  }
}
