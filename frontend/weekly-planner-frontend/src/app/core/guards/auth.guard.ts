import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Redirects to /identity if no user has been selected.
 * Applies to all protected routes that require an active user session.
 */
export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.currentUser$.pipe(
        take(1),
        map((user) => {
            if (user) {
                return true;
            }
            return router.createUrlTree(['/identity']);
        })
    );
};
