import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { CycleService } from '../../core/services/cycle.service';
import { Cycle } from '../../shared/models/cycle.model';

@Component({
  selector: 'app-past-cycles',
  standalone: true,
  imports: [DatePipe, RouterLink, NgClass,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDividerModule, MatSnackBarModule],
  templateUrl: './past-cycles.component.html',
  styleUrl: './past-cycles.component.scss',
})
export class PastCyclesComponent implements OnInit {
  private readonly cycleService = inject(CycleService);
  private readonly snackBar = inject(MatSnackBar);

  readonly cycles = signal<Cycle[]>([]);
  readonly isLoading = signal(true);

  ngOnInit(): void {
    this.cycleService.getHistory().subscribe({
      next: (cycles) => {
        // Sort newest first
        this.cycles.set([...cycles].sort((a, b) => b.weekStartDate.localeCompare(a.weekStartDate)));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load cycle history.', 'Close', { duration: 4000 });
      },
    });
  }

  cycleStatusCls(status: string): string {
    return {
      Completed: 'badge-cycle-completed',
      Frozen: 'badge-cycle-frozen',
      Planning: 'badge-cycle-open',
    }[status] ?? '';
  }

  memberCount(cycle: Cycle): number { return cycle.members?.length ?? 0; }
}
