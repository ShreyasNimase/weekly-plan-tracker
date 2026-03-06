import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { TeamMember } from '../../shared/models/team-member.model';
import { environment } from '../../../environments/environment';

const SESSION_USER_KEY = 'wp_selected_user_id';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/team-members`;

    private readonly _currentUser = new BehaviorSubject<TeamMember | null>(null);

    /** Emits the currently selected user (or null if none selected) */
    readonly currentUser$ = this._currentUser.asObservable();

    /** Emits true when the current user is a lead */
    readonly isLead$ = this._currentUser.pipe(map((u) => !!u?.isLead));

    /** Snapshot of the current user */
    get currentUser(): TeamMember | null {
        return this._currentUser.value;
    }

    /** Select a team member as the active user — persisted across refresh */
    selectUser(member: TeamMember): void {
        sessionStorage.setItem(SESSION_USER_KEY, member.id);
        this._currentUser.next(member);
    }

    /** Clear the current user selection */
    clearUser(): void {
        sessionStorage.removeItem(SESSION_USER_KEY);
        this._currentUser.next(null);
    }

    /**
     * Restores the previously selected user from sessionStorage.
     * Returns true if restored, false if no saved session.
     * Call this once at app startup (app.config or APP_INITIALIZER).
     */
    restoreSession(): Observable<boolean> {
        const savedId = sessionStorage.getItem(SESSION_USER_KEY);
        if (!savedId) {
            return new Observable((obs) => { obs.next(false); obs.complete(); });
        }
        return this.http.get<TeamMember>(`${this.apiUrl}/${savedId}`).pipe(
            tap((member) => this._currentUser.next(member)),
            map(() => true),
            // If user was removed from DB, clear the stale session
        );
    }

    /** Load a fresh copy of the selected user from the API */
    refreshCurrentUser(): Observable<TeamMember> {
        const id = this._currentUser.value?.id;
        if (!id) throw new Error('No user selected');
        return this.http
            .get<TeamMember>(`${this.apiUrl}/${id}`)
            .pipe(tap((member) => this._currentUser.next(member)));
    }

    /**
     * After a makeLead / deactivate operation, update currentUser in-memory
     * if the affected member is the logged-in user.
     */
    patchCurrentUserIfSelf(updated: TeamMember): void {
        if (updated.id === this._currentUser.value?.id) {
            this._currentUser.next(updated);
            sessionStorage.setItem(SESSION_USER_KEY, updated.id);
        }
    }
}
