import { Component, inject, computed } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter, map } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavbarComponent } from './layout/navbar/navbar.component';
import { FooterComponent } from './layout/footer/footer.component';

/** Routes where the navbar should be hidden */
const NAVBAR_HIDDEN_ROUTES = ['/setup', '/identity'];
/** Routes where the footer should be hidden */
const FOOTER_HIDDEN_ROUTES = ['/setup', '/identity'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent],
  template: `
    @if (showNavbar()) {
      <app-navbar />
    }
    <main [class.with-navbar]="showNavbar()">
      <router-outlet />
    </main>
    @if (showFooter()) {
      <app-footer />
    }
  `,
  styles: [`
    main {
      min-height: 100vh;
    }
    main.with-navbar {
      min-height: calc(100vh - 58px);
    }
  `],
})
export class App {
  private readonly router = inject(Router);

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  readonly showNavbar = computed(() => {
    const url = this.currentUrl();
    return !NAVBAR_HIDDEN_ROUTES.some((r) => url.startsWith(r));
  });

  readonly showFooter = computed(() => {
    const url = this.currentUrl();
    return !FOOTER_HIDDEN_ROUTES.some((r) => url.startsWith(r));
  });
}
