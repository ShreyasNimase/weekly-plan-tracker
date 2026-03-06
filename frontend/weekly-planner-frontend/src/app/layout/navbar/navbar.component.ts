import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-navbar',
    standalone: true,
    imports: [
        RouterLink,
        MatToolbarModule,
        MatButtonModule,
        MatIconModule,
        MatTooltipModule,
    ],
    templateUrl: './navbar.component.html',
    styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    readonly currentUser = toSignal(this.authService.currentUser$);
    readonly isLead = toSignal(this.authService.isLead$);
    readonly isDark = signal(false);

    /** Tracks current URL to conditionally hide the Home link */
    private readonly currentUrl = toSignal(
        this.router.events.pipe(
            filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        ),
        { initialValue: null }
    );

    isOnHomeOrIdentity(): boolean {
        const url = this.router.url;
        return url === '/home' || url.startsWith('/identity');
    }

    toggleTheme(): void {
        this.isDark.update((v) => !v);
        document.body.classList.toggle('dark-theme', this.isDark());
    }

    get userInitials(): string {
        const name = this.currentUser()?.name ?? '';
        return name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2);
    }
}
