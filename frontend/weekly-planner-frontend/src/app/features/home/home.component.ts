import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { CycleService } from '../../core/services/cycle.service';
import { Cycle } from '../../shared/models/cycle.model';
import { toSignal } from '@angular/core/rxjs-interop';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';
import { CycleStatusCardComponent } from '../../shared/components/cycle-status-card/cycle-status-card.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatSnackBarModule,
    CycleStatusCardComponent,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly cycleService = inject(CycleService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly currentUser = toSignal(this.authService.currentUser$);
  readonly activeCycle = toSignal(this.cycleService.activeCycle$);
  readonly isLoading = signal(true);

  // ── Computed helpers ────────────────────────────────────────────────────────

  /** True when the signed-in user is a Team Lead */
  get isLead(): boolean {
    return this.currentUser()?.isLead ?? false;
  }

  /** Cycle state (UPPER_CASE as returned by backend) */
  get cycleState(): string {
    return this.activeCycle()?.state ?? '';
  }

  get hasCycle(): boolean {
    return !!this.activeCycle();
  }

  /** True if current user's ID is in the cycle's participatingMemberIds */
  get isParticipating(): boolean {
    const userId = this.currentUser()?.id;
    const ids = this.activeCycle()?.participatingMemberIds ?? [];
    return !!(userId && ids.includes(userId));
  }

  get firstName(): string {
    return this.currentUser()?.name?.split(' ')[0] ?? '';
  }

  // ── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.cycleService.loadActive().subscribe({
      next: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false),
    });
  }

  // ── Navigation ───────────────────────────────────────────────────────────────

  goToSetup(): void { this.router.navigate(['/cycle/setup']); }
  goToBacklog(): void { this.router.navigate(['/backlog']); }
  goToTeam(): void { this.router.navigate(['/team']); }
  goToPastCycles(): void { this.router.navigate(['/past-cycles']); }
  goToPlanning(): void { this.router.navigate(['/planning']); }
  goToFreezeReview(): void { this.router.navigate(['/freeze-review']); }
  goToProgress(): void { this.router.navigate(['/progress']); }
  goToDashboard(): void {
    const id = this.activeCycle()?.id;
    if (id) this.router.navigate(['/dashboard', id]);
  }

  // ── Cycle actions ─────────────────────────────────────────────────────────

  startNewWeek(): void {
    if (this.activeCycle()) {
      this.snack('There is already an active week. Finish or cancel it first.', true);
      return;
    }
    // Backend auto-calculates next Tuesday; weekStartDate is ignored server-side
    this.cycleService.startCycle({ weekStartDate: '' }).subscribe({
      next: () => this.router.navigate(['/cycle/setup']),
      error: (err) => this.snack(err?.error?.message ?? 'Failed to start cycle.', true),
    });
  }

  confirmCancelSetup(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: 'Cancel This Setup?',
        message: 'The cycle will be removed. You can start a new one anytime.',
        confirmText: 'Yes, Cancel Setup',
        confirmColor: 'warn',
        icon: 'warning',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      const cycle = this.activeCycle();
      if (!cycle) return;
      this.cycleService.cancelCycle(cycle.id).subscribe({
        next: () => {
          this.snack('Setup cancelled.');
          this.cycleService.loadActive().subscribe();
        },
        error: (err) => this.snack(err?.error?.message ?? 'Cancel failed.', true),
      });
    });
  }

  confirmCancelPlanning(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: "Cancel This Week's Planning?",
        message: 'This will erase ALL assignments for every team member. Backlog items will go back to Available. This cannot be undone.',
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
          this.snack('Planning cancelled. All assignments cleared.');
          this.cycleService.loadActive().subscribe();
        },
        error: (err) => this.snack(err?.error?.message ?? 'Cancel failed.', true),
      });
    });
  }

  confirmFinishWeek(): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '440px',
      data: {
        title: 'Finish This Week?',
        message: 'This will mark the week as complete. Completed tasks will be archived. Unfinished tasks return to the backlog.',
        confirmText: 'Yes, Finish the Week',
        confirmColor: 'primary',
        icon: 'task_alt',
      },
    }).afterClosed().subscribe((confirmed: boolean) => {
      if (!confirmed) return;
      const cycle = this.activeCycle();
      if (!cycle) return;
      this.cycleService.completeCycle(cycle.id).subscribe({
        next: () => {
          this.snack('✓ Week complete! Great work, team.');
          this.cycleService.loadActive().subscribe();
          this.router.navigate(['/home']);
        },
        error: (err) => this.snack(err?.error?.message ?? 'Failed to complete cycle.', true),
      });
    });
  }

  // ── Private ──────────────────────────────────────────────────────────────────

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', {
      duration: isError ? 6000 : 4000,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
