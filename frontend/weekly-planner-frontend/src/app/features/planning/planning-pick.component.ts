import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgClass } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { CycleService } from '../../core/services/cycle.service';
import { ProgressService } from '../../core/services/progress.service';
import { AuthService } from '../../core/services/auth.service';
import { BacklogService } from '../../core/services/backlog.service';
import { BacklogItem } from '../../shared/models/backlog-item.model';
import { CycleMemberDetail, CategoryProgress, MemberProgress } from '../../shared/models/progress.model';
import { HourCommitModalComponent, HourCommitResult } from '../../shared/components/hour-commit-modal.component';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, of, switchMap, catchError } from 'rxjs';

interface BudgetPill {
    category: string;
    label: string;
    cls: string;
    remaining: number;
}

const CAT_META: Record<string, { label: string; cls: string }> = {
    Feature: { label: 'Feature', cls: 'cat-feature' },
    Bug: { label: 'Bug', cls: 'cat-bug' },
    TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
    Learning: { label: 'Learning', cls: 'cat-learning' },
    Other: { label: 'Other', cls: 'cat-other' },
};

@Component({
    selector: 'app-planning-pick',
    standalone: true,
    imports: [
        RouterLink,
        NgClass,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatDividerModule,
        MatSnackBarModule,
        MatDialogModule,
    ],
    templateUrl: './planning-pick.component.html',
    styleUrl: './planning-pick.component.scss',
})
export class PlanningPickComponent implements OnInit {
    private readonly cycleService = inject(CycleService);
    private readonly progressService = inject(ProgressService);
    private readonly authService = inject(AuthService);
    private readonly backlogService = inject(BacklogService);
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly router = inject(Router);

    readonly currentUser = toSignal(this.authService.currentUser$);

    readonly isLoading = signal(true);
    readonly availableItems = signal<BacklogItem[]>([]);
    readonly budgetPills = signal<BudgetPill[]>([]);
    readonly myHoursLeft = signal(30);
    readonly toastMsg = signal<string | null>(null);
    readonly addingId = signal<string | null>(null);

    private cycleMember: CycleMemberDetail | null = null;
    private cycleId = '';
    private myItemIds = new Set<string>();
    private toastTimer: ReturnType<typeof setTimeout> | null = null;

    get catMeta() { return CAT_META; }

    getCatMeta(cat: string) { return CAT_META[cat] ?? { label: cat, cls: 'cat-other' }; }

    ngOnInit(): void { this.load(); }

    load(): void {
        this.isLoading.set(true);
        const user = this.currentUser();

        this.cycleService.loadActive().pipe(
            switchMap((cycle) => {
                if (!cycle || !user) return of(null);
                this.cycleId = cycle.id;

                return forkJoin([
                    this.progressService.getCycleDetail(cycle.id).pipe(catchError(() => of(null))),
                    this.progressService.getCategoryProgress(cycle.id).pipe(catchError(() => of([]))),
                    this.backlogService.getAll({}).pipe(catchError(() => of([]))),
                ] as const);
            }),
            switchMap((result) => {
                if (!result) { this.isLoading.set(false); return of(null); }
                const [detail, catProgress, items] = result;

                // Category budget pills
                const pills: BudgetPill[] = (catProgress as CategoryProgress[]).map((cp) => {
                    const meta = CAT_META[cp.category] ?? { label: cp.category, cls: 'cat-other' };
                    return { category: cp.category, label: meta.label, cls: meta.cls, remaining: cp.remaining };
                });
                this.budgetPills.set(pills);

                // Store all backlog items (will use as context for modal)
                const allItems = (items as BacklogItem[]).filter(
                    (i) => i.status === 'Active' || i.status === 'Available'
                );

                const user = this.currentUser();
                if (!detail || !user) {
                    this.availableItems.set(allItems);
                    this.isLoading.set(false);
                    return of(null);
                }

                // Find this user's CycleMember
                const cm = (detail as any).members?.find(
                    (m: CycleMemberDetail) => m.teamMemberId === user.id
                ) ?? null;
                this.cycleMember = cm;

                if (!cm) {
                    this.availableItems.set(allItems);
                    this.isLoading.set(false);
                    return of(null);
                }

                // Load member's plan to exclude already-picked items and get hoursLeft
                return this.progressService.getMemberProgress(this.cycleId, cm.id).pipe(
                    catchError(() => of(null)),
                    switchMap((mp: MemberProgress | null) => {
                        if (mp?.tasks) {
                            this.myItemIds = new Set(mp.tasks.map((t) => t.backlogItemId));
                            this.myHoursLeft.set(mp.remainingHours);
                        }
                        // Filter out items already in plan
                        this.availableItems.set(allItems.filter((i) => !this.myItemIds.has(i.id)));
                        this.isLoading.set(false);
                        return of(null);
                    })
                );
            })
        ).subscribe({
            error: () => {
                this.isLoading.set(false);
                this.snackBar.open('Failed to load backlog.', 'Dismiss', { duration: 5000 });
            },
        });
    }

    pickItem(item: BacklogItem): void {
        const cm = this.cycleMember;
        if (!cm) return;

        const catPill = this.budgetPills().find((p) => p.category === item.category);

        this.dialog.open(HourCommitModalComponent, {
            width: '460px',
            data: {
                item,
                cycleMemberId: cm.id,
                myHoursLeft: this.myHoursLeft(),
                categoryBudgetLeft: catPill?.remaining ?? 30,
            },
        }).afterClosed().subscribe((res: HourCommitResult | { error: string } | null) => {
            if (!res) return;
            if ('error' in res) { this.snackBar.open(res.error, 'Close', { duration: 5000 }); return; }

            // Update local state
            this.myHoursLeft.update((h) => h - res.hours);
            this.availableItems.update((list) => list.filter((i) => i.id !== item.id));

            this.showToast(`Added: ${item.title}`);

            // Navigate back after brief delay so toast is visible
            setTimeout(() => this.router.navigate(['/planning']), 1200);
        });
    }

    categoryBudgetLeft(cat: string): number {
        return this.budgetPills().find((p) => p.category === cat)?.remaining ?? 30;
    }

    private showToast(msg: string): void {
        if (this.toastTimer) clearTimeout(this.toastTimer);
        this.toastMsg.set(msg);
        this.toastTimer = setTimeout(() => this.toastMsg.set(null), 3000);
    }
}
