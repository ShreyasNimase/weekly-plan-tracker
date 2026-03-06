import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { TeamMember } from '../../shared/models/team-member.model';
import { TeamService } from '../../core/services/team.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
    selector: 'app-identity',
    standalone: true,
    imports: [
        MatCardModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MatIconModule,
    ],
    templateUrl: './identity.component.html',
    styleUrl: './identity.component.scss',
})
export class IdentityComponent implements OnInit {
    private readonly teamService = inject(TeamService);
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);
    private readonly snackBar = inject(MatSnackBar);

    readonly members = signal<TeamMember[]>([]);
    readonly isLoading = signal(true);
    readonly hasError = signal(false);
    readonly selectedId = signal<string | null>(null);

    ngOnInit(): void {
        this.teamService.getAll().subscribe({
            next: (all) => {
                this.members.set(all.filter((m) => m.isActive));
                this.isLoading.set(false);
            },
            error: () => {
                this.isLoading.set(false);
                this.hasError.set(true);
                this.snackBar.open('Failed to load team members.', 'Dismiss', { duration: 5000 });
            },
        });
    }

    selectMember(member: TeamMember): void {
        this.selectedId.set(member.id);
        this.authService.selectUser(member);
        // Brief visual delay so the selected state is visible before navigating
        setTimeout(() => this.router.navigate(['/home']), 250);
    }

    getInitials(name: string): string {
        return name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2);
    }
}
