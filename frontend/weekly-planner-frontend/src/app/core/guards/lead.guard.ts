import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

/**
 * Restricts access to lead-only routes.
 * Redirects to /home if the current user is not a lead.
 */
export const leadGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.isLead$.pipe(
        take(1),
        map((isLead) => {
            if (isLead) {
                return true;
            }
            return router.createUrlTree(['/home']);
        })
    );
};
