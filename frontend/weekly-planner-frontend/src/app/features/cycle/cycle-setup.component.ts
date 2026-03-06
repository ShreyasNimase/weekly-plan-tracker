import {
  Component, inject, signal, OnInit, computed
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  ReactiveFormsModule, FormBuilder, Validators,
  AbstractControl, ValidationErrors
} from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, MAT_DATE_FORMATS, MAT_DATE_LOCALE } from '@angular/material/core';
import { NgClass } from '@angular/common';
import { CycleService } from '../../core/services/cycle.service';
import { TeamService } from '../../core/services/team.service';
import { TeamMember } from '../../shared/models/team-member.model';
import { Cycle } from '../../shared/models/cycle.model';
import { BacklogCategory } from '../../shared/enums/status.enum';
import { switchMap } from 'rxjs';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';

// ── Custom Tuesday validator ─────────────────────────────────────────────────
function tuesdayValidator(ctrl: AbstractControl): ValidationErrors | null {
  const v = ctrl.value;
  if (!v) return null;
  const d = v instanceof Date ? v : new Date(v + 'T12:00:00');
  return d.getDay() === 2 ? null : { mustBeTuesday: true };
}

// ── Custom date display format (dd-MM-yyyy) ────────────────────────────────────
const MY_DATE_FORMATS = {
  parse: { dateInput: 'DD/MM/YYYY' },
  display: {
    dateInput: 'DD/MM/YYYY',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'DD/MM/YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

// ── Category config ──────────────────────────────────────────────────────────
export const CAT_CONFIG = [
  { value: BacklogCategory.Feature, label: 'Client Focused', cls: 'cat-client', defaultPct: 50 },
  { value: BacklogCategory.TechDebt, label: 'Tech Debt', cls: 'cat-techdebt', defaultPct: 30 },
  { value: BacklogCategory.Learning, label: 'R&D', cls: 'cat-rnd', defaultPct: 20 },
];

@Component({
  selector: 'app-cycle-setup',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatDatepickerModule,
    MatNativeDateModule,
    NgClass,
  ],
  providers: [
    { provide: MAT_DATE_FORMATS, useValue: MY_DATE_FORMATS },
  ],
  templateUrl: './cycle-setup.component.html',
  styleUrl: './cycle-setup.component.scss',
})
export class CycleSetupComponent implements OnInit {
  private readonly cycleService = inject(CycleService);
  private readonly teamService = inject(TeamService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  readonly allMembers = signal<TeamMember[]>([]);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly activeCycle = signal<Cycle | null>(null);

  // Which members are currently checked (set of IDs)
  readonly checkedIds = signal<Set<string>>(new Set());

  // Category config exposed to template
  readonly catConfig = CAT_CONFIG;

  // ── Main form ──────────────────────────────────────────────────────────────
  readonly form = this.fb.group({
    weekStartDate: [this.getDefaultTuesdayDate(), [Validators.required, tuesdayValidator]],
    // Category percentages — fixed 3 fields
    pct0: [50, [Validators.required, Validators.min(0), Validators.max(100)]],
    pct1: [30, [Validators.required, Validators.min(0), Validators.max(100)]],
    pct2: [20, [Validators.required, Validators.min(0), Validators.max(100)]],
  });

  // ── Computed helpers ───────────────────────────────────────────────────────
  get selectedCount(): number { return this.checkedIds().size; }
  get totalHours(): number { return this.selectedCount * 30; }

  get pctValues(): number[] {
    const v = this.form.value;
    return [+(v.pct0 ?? 0), +(v.pct1 ?? 0), +(v.pct2 ?? 0)];
  }

  get pctTotal(): number { return this.pctValues.reduce((a, b) => a + b, 0); }
  get pctOk(): boolean { return this.pctTotal === 100; }

  get catHours(): number[] {
    return this.pctValues.map((p) => Math.round((p / 100) * this.totalHours));
  }

  get workPeriod(): string {
    const v = this.form.value.weekStartDate;
    if (!v || this.form.controls.weekStartDate.invalid) return '';
    const tue = v instanceof Date ? v : new Date((v as string) + 'T12:00:00');
    if (isNaN(tue.getTime())) return '';
    const wed = new Date(tue); wed.setDate(tue.getDate() + 1);
    const mon = new Date(tue); mon.setDate(tue.getDate() + 6);
    const dp = new DatePipe('en-GB');
    return `Work period: ${dp.transform(wed, 'dd-MM-yyyy')} to ${dp.transform(mon, 'dd-MM-yyyy')}`;
  }

  get canSubmit(): boolean {
    return this.form.controls.weekStartDate.valid
      && this.selectedCount > 0
      && this.pctOk
      && !this.isSubmitting();
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.teamService.getAll().subscribe({
      next: (members) => {
        const active = members.filter((m) => m.isActive);
        this.allMembers.set(active);
        // All checked by default
        this.checkedIds.set(new Set(active.map((m) => m.id)));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack('Failed to load team members.', true);
      },
    });
  }

  // ── Member checkbox toggle ─────────────────────────────────────────────────
  toggleMember(id: string): void {
    this.checkedIds.update((s) => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isMemberChecked(id: string): boolean { return this.checkedIds().has(id); }

  // ── Submit: start → setup → open (chained) ────────────────────────────────
  onOpenPlanning(): void {
    if (!this.canSubmit) return;
    this.isSubmitting.set(true);

    const dateStr = this.form.value.weekStartDate;
    const d = dateStr instanceof Date ? dateStr : (dateStr ? new Date((dateStr as string) + 'T12:00:00') : null);
    const isoDate = d ? d.toISOString().substring(0, 10) : '';
    const memberIds = [...this.checkedIds()];
    const pctVals = this.pctValues;

    const categoryBudgets = this.catConfig.map((c, i) => ({
      category: c.value,
      percentage: pctVals[i],
    }));

    // Chain: start → setup → open
    this.cycleService.startCycle({ weekStartDate: isoDate })
      .pipe(
        switchMap((cycle) =>
          this.cycleService.setupCycle(cycle.id, { memberIds, categoryBudgets })
        ),
        switchMap((cycle) =>
          this.cycleService.openCycle(cycle.id)
        )
      )
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.snackBar.open('Planning is open! Team members can now plan their work.', 'Close', { duration: 4500 });
          this.router.navigate(['/home']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.snack(err?.error?.message ?? 'Failed to open planning. Please try again.', true);
        },
      });
  }

  // ── Utility ───────────────────────────────────────────────────────────────
  private getDefaultTuesdayDate(): Date {
    const today = new Date();
    const day = today.getDay();
    const diff = day === 2 ? 0 : (2 - day + 7) % 7;
    const tue = new Date(today);
    tue.setDate(today.getDate() + diff);
    tue.setHours(12, 0, 0, 0);
    return tue;
  }

  /** Legacy: kept for workPeriod getter but replaced by getDefaultTuesdayDate */
  private getDefaultTuesday(): Date { return this.getDefaultTuesdayDate(); }

  private fmt(d: Date): string {
    return d.toISOString().substring(0, 10);
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', {
      duration: isError ? 6000 : 4000,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
