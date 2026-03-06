import { Routes } from '@angular/router';
import { setupGuard } from './core/guards/setup.guard';
import { authGuard } from './core/guards/auth.guard';
import { leadGuard } from './core/guards/lead.guard';

export const routes: Routes = [
    // ── Root: check DB then route to /setup, /identity, or /home ──────────
    {
        path: '',
        loadComponent: () =>
            import('./features/startup/startup.component').then((m) => m.StartupComponent),
    },

    // ── Public routes (no session required) ───────────────────────────────
    {
        path: 'setup',
        loadComponent: () =>
            import('./features/setup/setup.component').then((m) => m.SetupComponent),
    },
    {
        path: 'identity',
        canActivate: [setupGuard],
        loadComponent: () =>
            import('./features/identity/identity.component').then(
                (m) => m.IdentityComponent
            ),
    },

    // ── Protected routes (require team + user) ────────────────────────────
    {
        path: 'home',
        canActivate: [setupGuard, authGuard],
        loadComponent: () =>
            import('./features/home/home.component').then((m) => m.HomeComponent),
    },
    {
        path: 'team',
        canActivate: [setupGuard, authGuard, leadGuard],
        loadComponent: () =>
            import('./features/team/team.component').then((m) => m.TeamComponent),
    },
    {
        path: 'backlog',
        canActivate: [setupGuard, authGuard],
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('./features/backlog/backlog.component').then(
                        (m) => m.BacklogComponent
                    ),
            },
            {
                path: 'edit',
                loadComponent: () =>
                    import('./features/backlog/backlog-edit.component').then(
                        (m) => m.BacklogEditComponent
                    ),
            },
            {
                path: 'edit/:id',
                loadComponent: () =>
                    import('./features/backlog/backlog-edit.component').then(
                        (m) => m.BacklogEditComponent
                    ),
            },
        ],
    },
    {
        path: 'cycle/setup',
        canActivate: [setupGuard, authGuard, leadGuard],
        loadComponent: () =>
            import('./features/cycle/cycle-setup.component').then(
                (m) => m.CycleSetupComponent
            ),
    },
    {
        path: 'planning',
        canActivate: [setupGuard, authGuard],
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('./features/planning/planning.component').then(
                        (m) => m.PlanningComponent
                    ),
            },
            {
                path: 'pick',
                loadComponent: () =>
                    import('./features/planning/planning-pick.component').then(
                        (m) => m.PlanningPickComponent
                    ),
            },
        ],
    },
    {
        path: 'freeze-review',
        canActivate: [setupGuard, authGuard, leadGuard],
        loadComponent: () =>
            import('./features/cycle/freeze-review.component').then(
                (m) => m.FreezeReviewComponent
            ),
    },
    {
        path: 'progress',
        canActivate: [setupGuard, authGuard],
        loadComponent: () =>
            import('./features/progress/progress.component').then(
                (m) => m.ProgressComponent
            ),
    },
    {
        path: 'dashboard',
        canActivate: [setupGuard, authGuard],
        loadComponent: () =>
            import('./features/dashboard/dashboard.component').then(
                (m) => m.DashboardComponent
            ),
    },
    {
        path: 'dashboard/:cycleId',
        canActivate: [setupGuard, authGuard],
        children: [
            {
                path: '',
                loadComponent: () =>
                    import('./features/dashboard/dashboard.component').then(
                        (m) => m.DashboardComponent
                    ),
            },
            {
                path: 'category/:cat',
                loadComponent: () =>
                    import('./features/dashboard/category-drill.component').then(
                        (m) => m.CategoryDrillComponent
                    ),
            },
            {
                path: 'member/:memberId',
                loadComponent: () =>
                    import('./features/dashboard/member-drill.component').then(
                        (m) => m.MemberDrillComponent
                    ),
            },
        ],
    },
    {
        path: 'past-cycles',
        canActivate: [setupGuard, authGuard],
        loadComponent: () =>
            import('./features/past-cycles/past-cycles.component').then(
                (m) => m.PastCyclesComponent
            ),
    },

    // ── Catch-all ────────────────────────────────────────────────────────
    { path: '**', redirectTo: '' },
];
