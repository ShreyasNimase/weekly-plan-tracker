import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  APP_INITIALIZER,
  inject,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { catchError, of } from 'rxjs';

import { routes } from './app.routes';
import { AuthService } from './core/services/auth.service';

/**
 * Restore the persisted user session on startup.
 * If the sessionStorage entry is stale (user deleted), silently clears it.
 */
function sessionInitializer(authService: AuthService) {
  return () =>
    authService.restoreSession().pipe(catchError(() => {
      authService.clearUser();
      return of(false);
    }));
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimations(),
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => sessionInitializer(authService),
      deps: [AuthService],
      multi: true,
    },
  ],
};
