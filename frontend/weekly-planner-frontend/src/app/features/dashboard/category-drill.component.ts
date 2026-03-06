import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { DatePipe, NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProgressService } from '../../core/services/progress.service';
import { CycleProgress, CategoryProgress, CycleDetail } from '../../shared/models/progress.model';
import { catchError, of, forkJoin } from 'rxjs';

@Component({
  selector: 'app-category-drill',
  standalone: true,
  imports: [RouterLink, DatePipe, NgClass, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatDividerModule, MatSnackBarModule],
  template: `
<div class="page-wrapper">
  <a [routerLink]="['/dashboard', cycleId]" class="back-link"><mat-icon>arrow_back</mat-icon> Dashboard</a>

  @if (isLoading()) {
    <div class="flex-center" style="padding:60px 0;"><mat-spinner diameter="48"></mat-spinner></div>
  } @else {
    <div class="page-header">
      <h1>{{ catLabel }} — Week of {{ planDate | date:'MMM d, y' }}</h1>
      <p class="subtitle">Budget: <strong>{{ budgetHours }}h</strong> · Done: <strong>{{ usedHours }}h</strong> · {{ utilPct }}%</p>
    </div>

    <div class="wpt-card">
      <div class="progress-track progress-bar-fat">
        <div class="progress-fill" [ngClass]="barCls" [style.width.%]="utilPct"></div>
      </div>
    </div>

    <p class="section-label">Assignments</p>
    <div class="wpt-card" style="padding:0;overflow:hidden">
      @for (t of tasks(); track t['assignmentId']; let last = $last) {
        <div class="item-row">
          <span class="row-title">{{ t['title'] }}</span>
          <span class="row-meta">{{ t['memberName'] }}</span>
          <span class="row-meta">{{ t['plannedHours'] }}h planned</span>
          @if (t['hoursCompleted'] !== undefined) {
            <span class="hours-done">{{ t['hoursCompleted'] }}h done</span>
          }
          <span class="badge" [ngClass]="statusCls(tStatus(t))">{{ statusLabel(tStatus(t)) }}</span>
        </div>
        @if (!last) { <mat-divider></mat-divider> }
      }
      @if (tasks().length === 0) { <p class="text-muted" style="text-align:center;padding:24px">No tasks in this category.</p> }
    </div>
  }
</div>`,
  styleUrl: './category-drill.component.scss',
})
export class CategoryDrillComponent implements OnInit {
  private readonly progressService = inject(ProgressService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly tasks = signal<Record<string, unknown>[]>([]);

  get cycleId(): string { return this.route.snapshot.paramMap.get('cycleId') ?? ''; }
  get cat(): string { return this.route.snapshot.paramMap.get('cat') ?? ''; }

  planDate = ''; catLabel = ''; budgetHours = 0; usedHours = 0;

  get utilPct(): number { return this.budgetHours ? Math.min(100, Math.round((this.usedHours / this.budgetHours) * 100)) : 0; }
  get barCls(): string { return this.utilPct >= 100 ? 'bar-green' : this.utilPct >= 50 ? 'bar-blue' : 'bar-orange'; }

  readonly CAT_LABELS: Record<string, string> = { Feature: 'Feature', Bug: 'Bug', TechDebt: 'Tech Debt', Learning: 'Learning', Other: 'Other' };

  statusCls(s: string): string {
    return ({ NOT_STARTED: 'badge-not-started', IN_PROGRESS: 'badge-in-progress', COMPLETED: 'badge-completed', BLOCKED: 'badge-blocked' } as Record<string, string>)[s] ?? 'badge-not-started';
  }
  statusLabel(s: string): string {
    return ({ NOT_STARTED: 'Not Started', IN_PROGRESS: 'In Progress', COMPLETED: 'Completed', BLOCKED: 'Blocked' } as Record<string, string>)[s] ?? s;
  }
  tStatus(t: Record<string, unknown>): string { return (t['progressStatus'] as string) ?? 'NOT_STARTED'; }
  tHours(t: Record<string, unknown>): number | null { const v = t['hoursCompleted']; return v !== undefined ? +(v as number) : null; }

  ngOnInit(): void {
    const id = this.cycleId; const cat = this.cat;
    this.catLabel = this.CAT_LABELS[cat] ?? cat;

    forkJoin({
      detail: this.progressService.getCycleDetail(id).pipe(catchError(() => of(null))),
      progress: this.progressService.getCycleProgress(id).pipe(catchError(() => of(null))),
      catProg: this.progressService.getCategoryProgress(id).pipe(catchError(() => of([]))),
    }).subscribe({
      next: ({ detail, progress, catProg }) => {
        this.planDate = (detail as CycleDetail | null)?.weekStartDate ?? '';
        const cp = (catProg as CategoryProgress[]).find((c) => c.category === cat);
        if (cp) { this.budgetHours = cp.budgetHours; this.usedHours = cp.usedHours; }
        const all: Record<string, unknown>[] = ((progress as CycleProgress | null)?.members?.flatMap((m: any) =>
          (m.tasks ?? []).filter((t: any) => t.category === cat).map((t: any) => ({ ...t, memberName: m.name }))
        ) ?? []) as Record<string, unknown>[];
        this.tasks.set(all);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load category detail.', 'Dismiss', { duration: 5000 });
      },
    });
  }
}
