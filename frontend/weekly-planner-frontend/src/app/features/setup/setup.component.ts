import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TeamService } from '../../core/services/team.service';
import { concatMap, from } from 'rxjs';

interface LocalMember {
    name: string;
    isLead: boolean;
}

@Component({
    selector: 'app-setup',
    standalone: true,
    imports: [
        FormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MatDividerModule,
        MatTooltipModule,
    ],
    templateUrl: './setup.component.html',
    styleUrl: './setup.component.scss',
})
export class SetupComponent {
    private readonly teamService = inject(TeamService);
    private readonly router = inject(Router);
    private readonly snackBar = inject(MatSnackBar);

    nameInput = '';
    readonly members = signal<LocalMember[]>([]);
    readonly isLoading = signal(false);

    get canDone(): boolean {
        return this.members().length > 0 && this.members().some((m) => m.isLead);
    }

    addMember(): void {
        const name = this.nameInput.trim();
        if (!name) return;

        // Ignore duplicate (case-insensitive)
        const dup = this.members().some((m) => m.name.toLowerCase() === name.toLowerCase());
        if (dup) {
            this.snack(`"${name}" is already in the list.`, true);
            return;
        }

        // First person added → auto Team Lead
        const isLead = this.members().length === 0;
        this.members.update((list) => [...list, { name, isLead }]);
        this.nameInput = '';
    }

    makeLead(index: number): void {
        this.members.update((list) =>
            list.map((m, i) => ({ ...m, isLead: i === index }))
        );
    }

    removeMember(index: number): void {
        this.members.update((list) => {
            const next = list.filter((_, i) => i !== index);
            // If we removed the lead, auto-assign lead to the first remaining member
            if (list[index].isLead && next.length > 0) {
                next[0] = { ...next[0], isLead: true };
            }
            return next;
        });
    }

    onDone(): void {
        if (!this.canDone) return;
        this.isLoading.set(true);

        // POST each member sequentially (concatMap preserves order)
        from(this.members())
            .pipe(concatMap((m) => this.teamService.create({ name: m.name, isLead: m.isLead })))
            .subscribe({
                error: (err) => {
                    this.isLoading.set(false);
                    this.snack(err?.error?.message ?? 'Failed to save team. Please try again.', true);
                },
                complete: () => {
                    this.snack('Team saved! Now pick who you are.', false);
                    this.router.navigate(['/identity']);
                },
            });
    }

    onEnterKey(event: Event): void {
        event.preventDefault();
        this.addMember();
    }

    private snack(msg: string, isError: boolean): void {
        this.snackBar.open(msg, 'Close', {
            duration: isError ? 5000 : 3000,
            panelClass: isError ? ['snack-error'] : [],
        });
    }
}
