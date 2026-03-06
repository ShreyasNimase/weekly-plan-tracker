import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TeamService } from '../../core/services/team.service';
import { AuthService } from '../../core/services/auth.service';
import { catchError, of } from 'rxjs';

/**
 * Startup component — loaded at the root path ''.
 *
 * DB-based startup logic (replaces localStorage check):
 *  1. GET /api/team-members
 *  2. members.length === 0  → /setup   (no team yet)
 *  3. members.length > 0
 *       user already selected (session restored) → /home
 *       no user selected                         → /identity
 */
@Component({
    selector: 'app-startup',
    standalone: true,
    imports: [MatProgressSpinnerModule],
    template: `
    <div style="display:flex;justify-content:center;align-items:center;min-height:100vh;">
      <mat-spinner diameter="48"></mat-spinner>
    </div>
  `,
})
export class StartupComponent implements OnInit {
    private readonly teamService = inject(TeamService);
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    ngOnInit(): void {
        this.teamService.getAll().pipe(
            catchError(() => of([]))
        ).subscribe((members) => {
            if (!members || members.length === 0) {
                this.router.navigate(['/setup'], { replaceUrl: true });
            } else if (this.authService.currentUser) {
                // Session was restored by APP_INITIALIZER
                this.router.navigate(['/home'], { replaceUrl: true });
            } else {
                this.router.navigate(['/identity'], { replaceUrl: true });
            }
        });
    }
}
