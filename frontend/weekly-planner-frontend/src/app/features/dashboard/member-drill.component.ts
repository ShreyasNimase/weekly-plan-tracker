import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProgressService } from '../../core/services/progress.service';
import { MemberProgress } from '../../shared/models/progress.model';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-member-drill',
  standalone: true,
  imports: [RouterLink, NgClass, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatDividerModule, MatSnackBarModule],
  template: `
<div class="page-wrapper">
  <a [routerLink]="['/dashboard', cycleId]" class="back-link"><mat-icon>arrow_back</mat-icon> Dashboard</a>

  @if (isLoading()) {
    <div class="flex-center" style="padding:60px 0;"><mat-spinner diameter="48"></mat-spinner></div>
  } @else if (!mp()) {
    <div class="banner danger" style="text-align:center;padding:40px"><span>Could not load member data.</span></div>
  } @else {
    <div class="member-drill-header wpt-card">
      <div class="member-avatar-lg">{{ mp()!.name.charAt(0).toUpperCase() }}</div>
      <div>
        <h1>{{ mp()!.name }}'s Week</h1>
        <p class="subtitle">
          {{ mp()!.plannedHours }}h planned · {{ completedHours }}h completed ({{ progressPct }}%) ·
          <span class="badge" [ngClass]="overallStatusCls">{{ overallStatusLabel }}</span>
        </p>
      </div>
    </div>

    <div class="wpt-card">
      <div class="progress-track progress-bar-fat">
        <div class="progress-fill" [ngClass]="barCls" [style.width.%]="progressPct"></div>
      </div>
    </div>

    <p class="section-label">Tasks</p>
    <div class="wpt-card" style="padding:0;overflow:hidden">
      @for (t of mp()!.tasks; track t.assignmentId; let last = $last) {
        <div class="item-row">
          <span class="row-title">{{ t.title }}</span>
          <span class="badge" [ngClass]="catCls(t.category)">{{ catLabel(t.category) }}</span>
          <span class="row-meta">{{ t.plannedHours }}h</span>
          @if (getHoursCompleted(t) !== null) {
            <span class="hours-done">{{ getHoursCompleted(t) }}h done</span>
          }
          <span class="badge" [ngClass]="statusCls(getProgressStatus(t))">{{ statusLabel(getProgressStatus(t)) }}</span>
          @if (getNotes(t)) {
            <p class="task-note">{{ getNotes(t) }}</p>
          }
        </div>
        @if (!last) { <mat-divider></mat-divider> }
      }
      @if (mp()!.tasks.length === 0) {
        <p class="text-muted" style="text-align:center;padding:24px">No tasks assigned.</p>
      }
    </div>
  }
</div>`,
  styleUrl: './category-drill.component.scss',
})
export class MemberDrillComponent implements OnInit {
  private readonly progressService = inject(ProgressService);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly isLoading = signal(true);
  readonly mp = signal<MemberProgress | null>(null);

  get cycleId(): string { return this.route.snapshot.paramMap.get('cycleId') ?? ''; }
  get memberId(): string { return this.route.snapshot.paramMap.get('memberId') ?? ''; }

  get completedHours(): number {
    return (this.mp()?.tasks ?? []).reduce((s, t) => s + this.getHoursCompletedNum(t), 0);
  }
  get progressPct(): number {
    const planned = this.mp()?.plannedHours ?? 0;
    return planned ? Math.min(100, Math.round((this.completedHours / planned) * 100)) : 0;
  }
  get barCls(): string {
    return this.progressPct >= 100 ? 'bar-green' : this.progressPct >= 50 ? 'bar-blue' : 'bar-orange';
  }
  get overallStatusLabel(): string {
    const tasks: any[] = this.mp()?.tasks ?? [];
    if (this.mp()?.isReady) return 'All Done';
    if (tasks.some((t) => t['progressStatus'] === 'BLOCKED')) return 'Has Blockers';
    if (tasks.some((t) => t['progressStatus'] === 'IN_PROGRESS')) return 'In Progress';
    return 'Not Started';
  }
  get overallStatusCls(): string {
    const lbl = this.overallStatusLabel;
    if (lbl === 'All Done') return 'badge-completed';
    if (lbl === 'Has Blockers') return 'badge-blocked';
    if (lbl === 'In Progress') return 'badge-in-progress';
    return 'badge-not-started';
  }

  getHoursCompleted(t: any): number | null { const v = t['hoursCompleted']; return v !== undefined ? +v : null; }
  getHoursCompletedNum(t: any): number { return +(t['hoursCompleted'] ?? 0); }
  getProgressStatus(t: any): string { return t['progressStatus'] ?? 'NOT_STARTED'; }
  getNotes(t: any): string { return t['notes'] ?? ''; }

  catCls(cat: string): string {
    const m: Record<string, string> = {
      CLIENT_FOCUSED: 'cat-client', TECH_DEBT: 'cat-techdebt', R_AND_D: 'cat-rnd',
      Feature: 'cat-client', TechDebt: 'cat-techdebt', Learning: 'cat-rnd',
    };
    return m[cat] ?? 'cat-other';
  }
  catLabel(cat: string): string {
    const m: Record<string, string> = {
      CLIENT_FOCUSED: 'Client Focused', TECH_DEBT: 'Tech Debt', R_AND_D: 'R\u0026D',
      Feature: 'Client Focused', TechDebt: 'Tech Debt', Learning: 'R\u0026D',
    };
    return m[cat] ?? cat;
  }
  statusCls(s: string): string {
    return ({ NOT_STARTED: 'badge-not-started', IN_PROGRESS: 'badge-in-progress', COMPLETED: 'badge-completed', BLOCKED: 'badge-blocked' } as Record<string, string>)[s] ?? 'badge-not-started';
  }
  statusLabel(s: string): string {
    return ({ NOT_STARTED: 'Not Started', IN_PROGRESS: 'In Progress', COMPLETED: 'Completed', BLOCKED: 'Blocked' } as Record<string, string>)[s] ?? s;
  }

  ngOnInit(): void {
    this.progressService.getMemberProgress(this.cycleId, this.memberId).pipe(catchError(() => of(null))).subscribe({
      next: (mp: MemberProgress | null) => { this.mp.set(mp); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.snackBar.open('Failed to load member detail.', 'Dismiss', { duration: 5000 }); },
    });
  }
}
