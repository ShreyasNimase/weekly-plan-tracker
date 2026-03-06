import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { CycleService } from '../../core/services/cycle.service';
import { DataService } from '../../core/services/data.service';
import { Cycle } from '../../shared/models/cycle.model';
import { toSignal } from '@angular/core/rxjs-interop';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';

export interface ActionCard {
  emoji: string;
  icon: string;
  title: string;
  desc: string;
  route?: string;
  action?: () => void;
  danger?: boolean;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly cycleService = inject(CycleService);
  private readonly dataService = inject(DataService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly currentUser = toSignal(this.authService.currentUser$);
  readonly activeCycle = toSignal(this.cycleService.activeCycle$);
  readonly isLoading = signal(true);

  /** Derived state helpers */
  get isLead(): boolean { return this.currentUser()?.isLead ?? false; }
  get cycleStatus(): string { return this.activeCycle()?.status ?? ''; }
  get hasCycle(): boolean { return !!this.activeCycle(); }

  ngOnInit(): void {
    this.cycleService.loadActive().subscribe({
      next: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false),
    });
  }

  /** Compute the correct set of cards based on role + cycle state */
  get cards(): ActionCard[] {
    const status = this.cycleStatus;

    if (this.isLead) {
      // ── Lead, No cycle ──────────────────────────────────────────────
      if (!this.hasCycle) {
        return [
          { emoji: '🚀', icon: 'rocket_launch', title: 'Start a New Week', desc: 'Set up a new planning cycle.', route: '/cycle/setup' },
          { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Add, edit, or browse work items.', route: '/backlog' },
          { emoji: '👥', icon: 'group', title: 'Manage Team Members', desc: 'Add or remove team members.', route: '/team' },
          { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
        ];
      }
      // ── Lead, Planning (open) ─────────────────────────────────────
      if (status === 'Planning') {
        return [
          { emoji: '👁️', icon: 'visibility', title: 'Review and Freeze the Plan', desc: 'Check everyone\'s hours and lock the plan.', route: '/freeze-review' },
          { emoji: '📝', icon: 'edit_note', title: 'Plan My Work', desc: 'Pick backlog items and commit hours.', route: '/planning' },
          { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Add, edit, or browse work items.', route: '/backlog' },
          { emoji: '👥', icon: 'group', title: 'Manage Team Members', desc: 'Add or remove team members.', route: '/team' },
          { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
          { emoji: '❌', icon: 'cancel', title: 'Cancel This Week\'s Planning', desc: 'Erase all plans and start over.', action: () => this.openCancelDialog(), danger: true },
        ];
      }
      // ── Lead, Frozen ──────────────────────────────────────────────
      if (status === 'Frozen') {
        return [
          { emoji: '🔒', icon: 'lock', title: 'Review Frozen Plan', desc: 'View the locked plan for this week.', route: '/freeze-review' },
          { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Add, edit, or browse work items.', route: '/backlog' },
          { emoji: '👥', icon: 'group', title: 'Manage Team Members', desc: 'Add or remove team members.', route: '/team' },
          { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
        ];
      }
      // ── Lead, Setup (cycle created, not configured) ───────────────
      return [
        { emoji: '⚙️', icon: 'settings', title: 'Configure This Week', desc: 'Assign team and set category budgets.', route: '/cycle/setup' },
        { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Add, edit, or browse work items.', route: '/backlog' },
        { emoji: '👥', icon: 'group', title: 'Manage Team Members', desc: 'Add or remove team members.', route: '/team' },
        { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
      ];
    }

    // ── Member, No cycle ───────────────────────────────────────────────
    if (!this.hasCycle) {
      return [
        { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Browse and view work items.', route: '/backlog' },
        { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
      ];
    }
    // ── Member, Planning ──────────────────────────────────────────────
    if (status === 'Planning') {
      return [
        { emoji: '📝', icon: 'edit_note', title: 'Plan My Work', desc: 'Pick backlog items and commit your 30 hours.', route: '/planning' },
        { emoji: '📋', icon: 'list_alt', title: 'Manage Backlog', desc: 'Browse and view work items.', route: '/backlog' },
        { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
      ];
    }
    // ── Member, Frozen ─────────────────────────────────────────────────
    if (status === 'Frozen') {
      return [
        { emoji: '📝', icon: 'trending_up', title: 'Update My Progress', desc: 'Log your task completion and progress.', route: '/progress' },
        { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
      ];
    }
    // Member in Setup cycle — nothing actionable yet
    return [
      { emoji: '📅', icon: 'history', title: 'View Past Weeks', desc: 'Look at completed planning cycles.', route: '/past-cycles' },
    ];
  }

  /** Info banner text */
  get infoBanner(): { text: string; type: 'info' | 'warn' | 'success' } | null {
    if (this.isLead) {
      if (!this.hasCycle) return { text: 'No planning weeks yet. Click "Start a New Week" to begin!', type: 'info' };
      if (this.cycleStatus === 'Planning') return { text: 'Planning is open! Team members can now plan their work.', type: 'success' };
      if (this.cycleStatus === 'Setup') return { text: 'Cycle created! Configure team members and category budgets to open planning.', type: 'warn' };
    } else {
      if (!this.hasCycle) return { text: 'There\'s no active plan for you right now. Check back on Tuesday or ask your Team Lead.', type: 'info' };
    }
    return null;
  }

  navigate(card: ActionCard): void {
    if (card.action) {
      card.action();
    } else if (card.route) {
      this.router.navigate([card.route]);
    }
  }

  /** Returns a left-border accent color per card type */
  getCardBorderColor(card: ActionCard): string {
    const colorMap: Record<string, string> = {
      rocket_launch: '#2563EB',
      list_alt: '#7C3AED',
      group: '#0891B2',
      history: '#6B7280',
      lock: '#16A34A',
      visibility: '#16A34A',
      edit_note: '#D97706',
      trending_up: '#D97706',
      cancel: '#EF4444',
      settings: '#6B7280',
    };
    if (card.danger) return '#EF4444';
    return colorMap[card.icon] ?? '#2563EB';
  }

  private openCancelDialog(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: 'Cancel This Week\'s Planning?',
        message: 'This will delete the current cycle and all plans. Team members will lose their task assignments. This cannot be undone.',
        confirmText: 'Yes, Cancel Planning',
        confirmColor: 'warn',
        icon: 'warning',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      const cycle = this.activeCycle();
      if (!cycle) return;
      this.cycleService.cancelCycle(cycle.id).subscribe({
        next: () => {
          this.snackBar.open('Planning cycle cancelled.', 'Close', { duration: 4000 });
          // Reload active cycle (will become null)
          this.cycleService.loadActive().subscribe();
        },
        error: (err) => {
          this.snackBar.open(err?.error?.message ?? 'Cancel failed.', 'Dismiss', { duration: 5000, panelClass: ['snack-error'] });
        },
      });
    });
  }
}
