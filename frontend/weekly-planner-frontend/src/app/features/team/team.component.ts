import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { TeamMember } from '../../shared/models/team-member.model';
import { TeamService } from '../../core/services/team.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatDividerModule,
  ],
  templateUrl: './team.component.html',
  styleUrl: './team.component.scss',
})
export class TeamComponent implements OnInit {
  private readonly teamService = inject(TeamService);
  private readonly authService = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly members = signal<TeamMember[]>([]);
  readonly isLoading = signal(true);
  readonly isAdding = signal(false);

  readonly addForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
  });

  get currentUserId(): string | undefined {
    return this.authService.currentUser?.id;
  }

  get isLead(): boolean {
    return this.authService.currentUser?.isLead ?? false;
  }

  get activeMembers(): TeamMember[] {
    return this.members().filter((m) => m.isActive);
  }

  get inactiveMembers(): TeamMember[] {
    return this.members().filter((m) => !m.isActive);
  }

  /** True if there is exactly one lead (blocks removing the last lead) */
  get hasOnlyOneLead(): boolean {
    return this.members().filter((m) => m.isActive && m.isLead).length === 1;
  }

  ngOnInit(): void {
    this.loadMembers();
  }

  loadMembers(): void {
    this.isLoading.set(true);
    this.teamService.getAll().subscribe({
      next: (members) => {
        this.members.set(members);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snack('Failed to load team members', true);
      },
    });
  }

  addMember(): void {
    if (this.addForm.invalid) return;
    this.isAdding.set(true);

    this.teamService.create({ name: this.addForm.value.name!.trim(), isLead: false }).subscribe({
      next: (newMember) => {
        this.members.update((list) => [...list, newMember]);
        this.addForm.reset();
        this.isAdding.set(false);
        this.snack(`${newMember.name} added to the team`);
      },
      error: (err) => {
        this.isAdding.set(false);
        this.snack(err?.error?.message ?? 'Failed to add member', true);
      },
    });
  }

  makeLead(member: TeamMember): void {
    this.teamService.makeLead(member.id).subscribe({
      next: () => {
        this.snack(`${member.name} is now the Team Lead`);
        // Refresh list to reflect old lead → Member and new lead → Lead
        this.loadMembers();
        // If the current user was the old lead, refresh their session data
        this.authService.refreshCurrentUser().subscribe();
      },
      error: (err) => this.snack(err?.error?.message ?? 'Error making lead', true),
    });
  }

  deactivate(member: TeamMember): void {
    if (member.isLead && this.hasOnlyOneLead) {
      this.snack('Cannot remove the only Team Lead. Assign another lead first.', true);
      return;
    }

    this.teamService.deactivate(member.id).subscribe({
      next: () => {
        this.snack(`${member.name} has been removed`);
        this.loadMembers();
      },
      error: (err) => this.snack(err?.error?.message ?? 'Cannot remove member', true),
    });
  }

  reactivate(member: TeamMember): void {
    this.teamService.reactivate(member.id).subscribe({
      next: () => {
        this.snack(`${member.name} has been reactivated`);
        this.loadMembers();
      },
      error: (err) => this.snack(err?.error?.message ?? 'Error reactivating member', true),
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map((n) => n[0]).join('').toUpperCase().slice(0, 2);
  }

  private snack(msg: string, isError = false): void {
    this.snackBar.open(msg, 'Close', {
      duration: 4000,
      panelClass: isError ? ['snack-error'] : [],
    });
  }
}
