import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { DatePipe, NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProgressService } from '../../core/services/progress.service';
import { CycleService } from '../../core/services/cycle.service';
import { CycleProgress, MemberProgressSummary, CategoryProgress, CategoryProgressSummary } from '../../shared/models/progress.model';
import { CycleDetail } from '../../shared/models/progress.model';
import { forkJoin, of, catchError } from 'rxjs';

interface StatCard { label: string; value: string; sub?: string; borderCls: string; pct?: number; }

const CAT_META: Record<string, { label: string; cls: string }> = {
  Feature: { label: 'Feature', cls: 'cat-feature' },
  Bug: { label: 'Bug', cls: 'cat-bug' },
  TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
  Learning: { label: 'Learning', cls: 'cat-learning' },
  Other: { label: 'Other', cls: 'cat-other' },
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink, DatePipe, NgClass,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDividerModule, MatSnackBarModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly progressService = inject(ProgressService);
  private readonly cycleService = inject(CycleService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly cycle = signal<CycleDetail | null>(null);
  readonly cycleProgress = signal<CycleProgress | null>(null);
  readonly catProgress = signal<CategoryProgress[]>([]);
  readonly showAllTasks = signal(false);

  get cycleId(): string { return this.route.snapshot.paramMap.get('cycleId') ?? ''; }
  get fromPast(): boolean { return this.route.snapshot.queryParamMap.get('from') === 'past'; }

  get planDate(): string { return this.cycle()?.weekStartDate ?? ''; }
  get cycleStatus(): string { return this.cycle()?.status ?? ''; }
  get totalMembers(): number { return this.cycle()?.members?.length ?? 0; }
  get totalHours(): number { return this.totalMembers * 30; }

  get statCards(): StatCard[] {
    const p = this.cycleProgress();
    if (!p) return [];
    const totalPlanned = p.totalPlannedHours;
    const totalAlloc = p.totalAllocatedHours || this.totalHours;
    const planPct = totalAlloc ? Math.round((totalPlanned / totalAlloc) * 100) : 0;
    const allTasks = p.members.flatMap((m) => (m as any).tasks ?? []);
    const completed = allTasks.filter((t: any) => t.progressStatus === 'COMPLETED').length;
    const blocked = allTasks.filter((t: any) => t.progressStatus === 'BLOCKED').length;
    const hoursCompleted = allTasks.reduce((s: number, t: any) => s + (t.hoursCompleted ?? 0), 0);

    return [
      { label: 'Total Hours Planned', value: `${totalPlanned}h`, sub: `of ${totalAlloc}h capacity`, borderCls: 'border-blue', pct: planPct },
      { label: 'Hours Completed', value: `${hoursCompleted}h`, sub: `${planPct}%`, borderCls: 'border-green' },
      { label: 'Tasks Completed', value: `${completed}`, sub: `of ${allTasks.length}`, borderCls: 'border-green-light' },
      { label: 'Blocked Tasks', value: `${blocked}`, sub: blocked > 0 ? 'Needs attention' : 'All clear', borderCls: blocked > 0 ? 'border-red' : 'border-grey' },
    ];
  }

  get catRows(): (CategoryProgress & { catLabel: string; catCls: string; pct: number; barCls: string })[] {
    return this.catProgress().map((cp) => {
      const meta = CAT_META[cp.category] ?? { label: cp.category, cls: 'cat-other' };
      const pct = cp.budgetHours ? Math.min(100, Math.round((cp.usedHours / cp.budgetHours) * 100)) : 0;
      return {
        ...cp,
        catLabel: meta.label, catCls: meta.cls,
        pct,
        barCls: pct >= 100 ? 'bar-green' : pct >= 50 ? 'bar-blue' : 'bar-orange',
      };
    });
  }

  memberRows(): MemberProgressSummary[] { return this.cycleProgress()?.members ?? []; }

  memberStatus(m: MemberProgressSummary): string {
    if (m.isReady) return 'All Done';
    if (m.plannedHours > 0) return 'In Progress';
    return 'Not Started';
  }
  memberStatusCls(m: MemberProgressSummary): string {
    if (m.isReady) return 'badge-completed';
    if (m.plannedHours > 0) return 'badge-in-progress';
    return 'badge-not-started';
  }
  memberPct(m: MemberProgressSummary): number {
    return m.allocatedHours ? Math.min(100, Math.round((m.plannedHours / m.allocatedHours) * 100)) : 0;
  }

  cycleStatusCls(s: string): string {
    return { Planning: 'badge-cycle-open', Frozen: 'badge-cycle-frozen', Completed: 'badge-cycle-completed' }[s] ?? '';
  }

  catMeta(cat: string) { return CAT_META[cat] ?? { label: cat, cls: 'cat-other' }; }

  allTasks(): any[] { return this.cycleProgress()?.members.flatMap((m: any) => m.tasks?.map((t: any) => ({ ...t, memberName: m.name })) ?? []) ?? []; }

  ngOnInit(): void {
    const id = this.cycleId;
    if (!id) { this.router.navigate(['/home']); return; }

    forkJoin([
      this.progressService.getCycleDetail(id).pipe(catchError(() => of(null))),
      this.progressService.getCycleProgress(id).pipe(catchError(() => of(null))),
      this.progressService.getCategoryProgress(id).pipe(catchError(() => of([]))),
    ] as const).subscribe({
      next: ([detail, progress, catProg]) => {
        this.cycle.set(detail as CycleDetail);
        if (progress) this.cycleProgress.set(progress as CycleProgress);
        this.catProgress.set(catProg as CategoryProgress[]);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load dashboard.', 'Dismiss', { duration: 5000 });
      },
    });
  }

  navigateCat(cat: string): void {
    this.router.navigate(['/dashboard', this.cycleId, 'category', cat]);
  }
  navigateMember(m: MemberProgressSummary): void {
    this.router.navigate(['/dashboard', this.cycleId, 'member', m.cycleMemberId]);
  }
  toggleAllTasks(): void { this.showAllTasks.update((v) => !v); }
}
