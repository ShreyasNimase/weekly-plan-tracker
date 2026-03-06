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
import { CategoryProgress } from '../../shared/models/progress.model';
import { HourCommitModalComponent, HourCommitResult } from '../../shared/components/hour-commit-modal.component';
import { toSignal } from '@angular/core/rxjs-interop';
import { forkJoin, of, catchError } from 'rxjs';

interface BudgetPill {
    category: string;
    label: string;
    cls: string;
    remaining: number;
}

const CAT_META: Record<string, { label: string; cls: string }> = {
    CLIENT_FOCUSED: { label: 'Client Focused', cls: 'cat-client' },
    TECH_DEBT: { label: 'Tech Debt', cls: 'cat-techdebt' },
    R_AND_D: { label: 'R\u0026D', cls: 'cat-rnd' },
    // legacy fallbacks
    Feature: { label: 'Client Focused', cls: 'cat-client' },
    TechDebt: { label: 'Tech Debt', cls: 'cat-techdebt' },
    Learning: { label: 'R\u0026D', cls: 'cat-rnd' },
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

    private memberPlanId: string | null = null;
    private cycleId = '';
    private myItemIds = new Set<string>();
    private toastTimer: ReturnType<typeof setTimeout> | null = null;

    get catMeta() { return CAT_META; }

    getCatMeta(cat: string) { return CAT_META[cat] ?? { label: cat, cls: 'cat-other' }; }

    ngOnInit(): void { this.load(); }

    load(): void {
        this.isLoading.set(true);
        const user = this.currentUser();
        const cycle = this.cycleService.activeCycle;

        if (!cycle || !user) {
            this.isLoading.set(false);
            return;
        }

        this.cycleId = cycle.id;

        // ── Find this user's MemberPlan from the cached cycle ──────────────────
        // GET /api/cycles/{id} doesn't exist — use memberPlans embedded in activeCycle
        const memberPlan = cycle.memberPlans?.find(mp => mp.memberId === user.id);
        this.memberPlanId = memberPlan?.id ?? null;

        if (memberPlan) {
            this.myHoursLeft.set(30 - (memberPlan.totalPlannedHours ?? 0));
        }

        forkJoin([
            this.progressService.getCategoryProgress(cycle.id).pipe(catchError(() => of([]))),
            this.backlogService.getAll({}).pipe(catchError(() => of([]))),
            this.memberPlanId
                // Backend route: GET /api/cycles/{id}/members/{memberId}/progress (memberId = TeamMember.Id)
                ? this.progressService.getMemberProgress(cycle.id, user.id).pipe(catchError(() => of(null)))
                : of(null),
        ] as const).subscribe({
            next: ([catProgress, items, mp]) => {
                // Category budget pills
                const pills: BudgetPill[] = (catProgress as CategoryProgress[]).map((cp) => {
                    const meta = CAT_META[cp.category] ?? { label: cp.category, cls: 'cat-other' };
                    return { category: cp.category, label: meta.label, cls: meta.cls, remaining: cp.remaining };
                });
                this.budgetPills.set(pills);

                // Available items from backlog (AVAILABLE = not yet in any plan)
                const allAvailable = (items as BacklogItem[]).filter(i => i.status === 'AVAILABLE');

                if ((mp as any)?.tasks) {
                    this.myItemIds = new Set((mp as any).tasks.map((t: any) => t.backlogItemId));
                    this.myHoursLeft.set((mp as any).remainingHours ?? this.myHoursLeft());
                }

                this.availableItems.set(allAvailable.filter(i => !this.myItemIds.has(i.id)));
                this.isLoading.set(false);
            },
            error: () => {
                this.isLoading.set(false);
                this.snackBar.open('Failed to load backlog.', 'Dismiss', { duration: 5000 });
            },
        });
    }

    pickItem(item: BacklogItem): void {
        const memberPlanId = this.memberPlanId;
        if (!memberPlanId) {
            this.snackBar.open('You are not a member of this cycle.', 'Dismiss', { duration: 4000 });
            return;
        }

        const catPill = this.budgetPills().find((p) => p.category === item.category);

        this.dialog.open(HourCommitModalComponent, {
            width: '460px',
            data: {
                item,
                cycleMemberId: memberPlanId,   // MemberPlan.Id
                myHoursLeft: this.myHoursLeft(),
                categoryBudgetLeft: catPill?.remaining ?? 30,
            },
        }).afterClosed().subscribe((res: HourCommitResult | { error: string } | null) => {
            if (!res) return;
            if ('error' in res) { this.snackBar.open(res.error, 'Close', { duration: 5000 }); return; }

            this.myHoursLeft.update((h) => h - res.hours);
            this.availableItems.update((list) => list.filter((i) => i.id !== item.id));
            this.showToast(`Added: ${item.title}`);
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
