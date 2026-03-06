import {
  Component, inject, signal, OnInit, computed
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  ReactiveFormsModule, FormBuilder, Validators,
  AbstractControl, ValidationErrors
} from '@angular/forms';
import { DatePipe, NgClass } from '@angular/common';
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
import { MatNativeDateModule, MAT_DATE_FORMATS } from '@angular/material/core';
import { CycleService } from '../../core/services/cycle.service';
import { TeamService } from '../../core/services/team.service';
import { TeamMember } from '../../shared/models/team-member.model';
import { Cycle } from '../../shared/models/cycle.model';
import { switchMap, of } from 'rxjs';

// ── Custom date display format (dd/MM/yyyy) ───────────────────────────────────
const MY_DATE_FORMATS = {
  parse: { dateInput: 'DD/MM/YYYY' },
  display: {
    dateInput: 'DD/MM/YYYY',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'DD/MM/YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};

// ── Category config ───────────────────────────────────────────────────────────
export const CAT_CONFIG = [
  { value: 'CLIENT_FOCUSED', label: 'Client Focused', cls: 'cat-client', defaultPct: 50 },
  { value: 'TECH_DEBT', label: 'Tech Debt', cls: 'cat-techdebt', defaultPct: 30 },
  { value: 'R_AND_D', label: 'R&D', cls: 'cat-rnd', defaultPct: 20 },
];

@Component({
  selector: 'app-cycle-setup',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    NgClass,
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
  readonly submitError = signal<string | null>(null);

  /** Which members are currently checked (set of IDs) */
  readonly checkedIds = signal<Set<string>>(new Set());

  /** Category config exposed to template */
  readonly catConfig = CAT_CONFIG;

  // ── Main form ─────────────────────────────────────────────────────────────
  readonly form = this.fb.group({
    weekStartDate: [this.getDefaultTuesdayDate(), [Validators.required, this.isTuesdayValidator.bind(this)]],
    // Category percentages — min 1% each so no category gets 0h
    pct0: [50, [Validators.required, Validators.min(1), Validators.max(98)]],
    pct1: [30, [Validators.required, Validators.min(1), Validators.max(98)]],
    pct2: [20, [Validators.required, Validators.min(1), Validators.max(98)]],
  });

  // ── Computed helpers ──────────────────────────────────────────────────────
  get selectedMemberIds(): string[] { return [...this.checkedIds()]; }
  get selectedCount(): number { return this.checkedIds().size; }
  get totalHours(): number { return this.selectedCount * 30; }

  get pctClient(): number { return +(this.form.value.pct0 ?? 0); }
  get pctTech(): number { return +(this.form.value.pct1 ?? 0); }
  get pctRD(): number { return +(this.form.value.pct2 ?? 0); }

  get pctValues(): number[] { return [this.pctClient, this.pctTech, this.pctRD]; }

  pctSum(): number { return this.pctValues.reduce((a, b) => a + b, 0); }

  get catHours(): number[] {
    return this.pctValues.map((p) => Math.round((p / 100) * this.totalHours));
  }

  /** 'valid' | 'wrong-total' | 'zero-category' */
  getPctStatus(): 'valid' | 'wrong-total' | 'zero-category' {
    if (this.pctSum() !== 100) return 'wrong-total';
    if (this.pctClient < 1 || this.pctTech < 1 || this.pctRD < 1) return 'zero-category';
    return 'valid';
  }

  calcBudget(cat: string): number {
    const idx = this.catConfig.findIndex(c => c.value === cat);
    if (idx < 0) return 0;
    return Math.round((this.pctValues[idx] / 100) * this.totalHours);
  }

  isFormValid(): boolean {
    return (
      this.isValidTuesday(this.form.value.weekStartDate) &&
      this.selectedCount > 0 &&
      this.pctSum() === 100 &&
      this.pctClient >= 1 &&
      this.pctTech >= 1 &&
      this.pctRD >= 1
    );
  }

  get canSubmit(): boolean {
    return this.isFormValid() && !this.isSubmitting();
  }

  // ── Date helpers ─────────────────────────────────────────────────────────

  /** Tuesday validator bound as instance method so `this` is accessible */
  isTuesdayValidator(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;
    const str = this.formatDateForCheck(control.value);
    const d = new Date(str + 'T12:00:00');
    return d.getDay() === 2 ? null : { notTuesday: true };
  }

  private formatDateForCheck(value: any): string {
    if (!value) return '';
    // Always build from LOCAL date parts to avoid UTC timezone shift
    let d: Date;
    if (value instanceof Date) {
      d = value;
    } else if (typeof value === 'string') {
      // If already ISO string like "2026-03-10", parse with noon to stay local
      d = new Date(value.slice(0, 10) + 'T12:00:00');
    } else {
      d = new Date(value);
    }
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  isValidTuesday(value: any): boolean {
    if (!value) return false;
    const str = this.formatDateForCheck(value);
    const d = new Date(str + 'T12:00:00');
    return d.getDay() === 2;
  }

  /** BUG 1 FIX: Returns a Date object — NOT a "Work period: ..." string */
  getWorkStart(): Date {
    const d = new Date(this.formatDateForCheck(this.form.value.weekStartDate) + 'T12:00:00');
    d.setDate(d.getDate() + 1); // Wednesday
    return d;
  }

  getWorkEnd(): Date {
    const d = new Date(this.formatDateForCheck(this.form.value.weekStartDate) + 'T12:00:00');
    d.setDate(d.getDate() + 6); // Monday
    return d;
  }

  getDayName(value: any): string {
    if (!value) return '';
    const str = this.formatDateForCheck(value);
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[new Date(str + 'T12:00:00').getDay()];
  }

  formatDisplay(value: any): string {
    if (!value) return '';
    const str = this.formatDateForCheck(value);
    const [y, m, d] = str.split('-');
    return `${d}-${m}-${y}`;
  }

  // ── Quick preset ─────────────────────────────────────────────────────────
  setPreset(a: number, b: number, c: number): void {
    this.form.patchValue({ pct0: a, pct1: b, pct2: c });
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    // State guard: this page requires an active cycle in SETUP state
    const existingCycle = this.cycleService.activeCycle;
    if (existingCycle && existingCycle.state !== 'SETUP') {
      this.snack('No cycle awaiting setup.', true);
      this.router.navigate(['/home']);
      return;
    }

    this.teamService.getAll().subscribe({
      next: (members) => {
        const active = members.filter((m) => m.isActive);
        this.allMembers.set(active);
        this.checkedIds.set(new Set(active.map((m) => m.id)));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack('Failed to load team members.', true);
      },
    });
  }

  // ── Member checkbox toggle ────────────────────────────────────────────────
  toggleMember(id: string): void {
    this.checkedIds.update((s) => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isMemberChecked(id: string): boolean { return this.checkedIds().has(id); }

  // ── Submit: start → setup → open (chained) ───────────────────────────────
  onOpenPlanning(): void {
    if (!this.canSubmit) return;
    this.isSubmitting.set(true);
    this.submitError.set(null);

    const dateStr = this.form.value.weekStartDate;
    // Use LOCAL date parts to avoid UTC timezone shift (e.g. IST midnight → UTC prev day)
    const d = dateStr instanceof Date
      ? dateStr
      : (dateStr ? new Date((dateStr as string).slice(0, 10) + 'T12:00:00') : null);
    const isoDate = d
      ? `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
      : '';
    const memberIds = [...this.checkedIds()];
    const pctVals = this.pctValues;

    const categoryAllocations = this.catConfig.map((c, i) => ({
      category: c.value,
      percentage: pctVals[i],
    }));

    // If there's already a SETUP cycle (e.g. previous partial submit), reuse it
    // instead of calling startCycle again — avoids "already being planned" error.
    const cached = this.cycleService.activeCycle;
    const cachedState = cached?.state ?? cached?.status ?? '';
    const isAlreadySetup = cached && ['SETUP', 'Setup'].includes(cachedState);

    const start$ = isAlreadySetup
      ? of(cached!)                                          // reuse existing cycle
      : this.cycleService.startCycle({ weekStartDate: isoDate });

    start$
      .pipe(
        switchMap((cycle) =>
          this.cycleService.setupCycle(cycle.id, {
            planningDate: isoDate,          // ← required by backend, must be the Tuesday
            memberIds,
            categoryAllocations,
          })
        ),
        switchMap((res: any) => {
          const cycleId = (res?.data ?? res)?.id ?? this.cycleService.activeCycle?.id ?? '';
          return this.cycleService.openCycle(cycleId);
        }),
        switchMap(() => this.cycleService.loadActive())
      )
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.snackBar.open('✓ Planning is now open! Team members can now plan their work.', 'Close', { duration: 4500 });
          this.router.navigate(['/home']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.cycleService.loadActive().subscribe();
          const msg = err?.error?.message ?? 'Failed to open planning. Please try again.';
          this.submitError.set(msg);
          this.snack(msg, true);
        },
      });
  }

  // ── Utility ──────────────────────────────────────────────────────────────
  private getDefaultTuesdayDate(): Date {
    const today = new Date();
    const day = today.getDay();
    const diff = day === 2 ? 0 : (2 - day + 7) % 7;
    const tue = new Date(today);
    tue.setDate(today.getDate() + diff);
    tue.setHours(12, 0, 0, 0);
    return tue;
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, isError ? 'Dismiss' : 'Close', {
      duration: isError ? 6000 : 4000,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
