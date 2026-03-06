import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { TeamService } from '../services/team.service';

/**
 * SetupGuard — blocks access to protected routes if no team members exist.
 *
 * Used on /identity, /home, /team, /backlog, etc.
 * If the DB has no members → redirect to /setup.
 * Otherwise → allow navigation.
 */
export const setupGuard: CanActivateFn = () => {
    const teamService = inject(TeamService);
    const router = inject(Router);

    return teamService.getAll().pipe(
        map((members) => {
            if (members && members.length > 0) {
                return true;
            }
            return router.createUrlTree(['/setup']);
        }),
        catchError(() => of(router.createUrlTree(['/setup'])))
    );
};
