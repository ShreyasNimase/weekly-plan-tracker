import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DataService } from '../../core/services/data.service';
import { AuthService } from '../../core/services/auth.service';
import { CycleService } from '../../core/services/cycle.service';
import { ConfirmDialogComponent } from '../../shared/dialogs/confirm-dialog.component';
import { ImportDialogComponent } from '../../shared/dialogs/import-dialog.component';

@Component({
    selector: 'app-footer',
    standalone: true,
    imports: [
        MatButtonModule,
        MatIconModule,
        MatDialogModule,
        MatSnackBarModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
    ],
    templateUrl: './footer.component.html',
    styleUrl: './footer.component.scss',
})
export class FooterComponent {
    private readonly dataService = inject(DataService);
    private readonly authService = inject(AuthService);
    private readonly cycleService = inject(CycleService);
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly router = inject(Router);

    readonly isDownloading = signal(false);

    // ── 1. Download My Data ──────────────────────────────────────
    download(): void {
        this.isDownloading.set(true);
        this.dataService.downloadBackup().subscribe({
            next: (blob) => {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                const date = new Date().toISOString().slice(0, 10);
                a.href = url;
                a.download = `weekly-planner-backup-${date}.json`;
                a.click();
                URL.revokeObjectURL(url);
                this.isDownloading.set(false);
                this.snack('Backup downloaded!');
            },
            error: () => {
                this.isDownloading.set(false);
                this.snack('Download failed.', true);
            },
        });
    }

    // ── 2. Load Data from File ────────────────────────────────────
    openImportDialog(): void {
        this.dialog.open(ImportDialogComponent, {
            width: '500px',
            disableClose: true,
        }).afterClosed().subscribe((imported: boolean) => {
            if (imported) {
                this.authService.clearUser();
                this.cycleService.clearActive();
                this.snack('Data imported! Please select your identity again.');
                this.router.navigate(['/identity']);
            }
        });
    }

    // ── 3. Seed Sample Data ───────────────────────────────────────
    openSeedDialog(): void {
        this.dialog.open(ConfirmDialogComponent, {
            width: '440px',
            data: {
                title: 'Load Sample Data',
                message: 'This will REPLACE all existing data with 4 team members and 10 backlog items. Any current team, backlog, cycles, and assignments will be erased.',
                confirmText: 'Replace & Seed',
                confirmColor: 'primary',
                icon: 'science',
            },
        }).afterClosed().subscribe((confirmed: boolean) => {
            if (!confirmed) return;
            this.dataService.seedData().subscribe({
                next: () => {
                    // Clear all frontend state
                    this.authService.clearUser();
                    this.cycleService.clearActive();
                    this.snack('✓ Sample data loaded! Pick a person to get started.');
                    this.router.navigate(['/identity']);
                },
                error: (err) => this.snack(err?.error?.message ?? 'Seed failed.', true),
            });
        });
    }

    // ── 4. Reset App ──────────────────────────────────────────────
    openResetDialog(): void {
        this.dialog.open(ConfirmDialogComponent, {
            width: '440px',
            data: {
                title: 'Reset Everything?',
                message: 'This will permanently erase ALL data — team members, backlog, cycles, and assignments. This cannot be undone.',
                confirmText: 'Yes, Reset Everything',
                confirmColor: 'warn',
                icon: 'warning',
            },
        }).afterClosed().subscribe((confirmed: boolean) => {
            if (!confirmed) return;
            this.dataService.resetApp().subscribe({
                next: () => {
                    // Clear all frontend state
                    this.authService.clearUser();
                    this.cycleService.clearActive();
                    sessionStorage.clear();
                    localStorage.removeItem('wpt-theme');
                    this.snack('App reset. Starting fresh.');
                    this.router.navigate(['/setup']);
                },
                error: (err) => this.snack(err?.error?.message ?? 'Reset failed.', true),
            });
        });
    }

    private snack(msg: string, isError = false): void {
        this.snackBar.open(msg, 'Close', {
            duration: isError ? 6000 : 3500,
            panelClass: isError ? ['snack-error'] : [],
        });
    }
}
