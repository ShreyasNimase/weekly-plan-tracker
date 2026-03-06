import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, of } from 'rxjs';
import { Cycle, StartCycleDto, SetupCycleDto } from '../../shared/models/cycle.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CycleService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/cycles`;

    private readonly _activeCycle = new BehaviorSubject<Cycle | null>(null);

    /** Emits the current active cycle (or null if none) */
    readonly activeCycle$ = this._activeCycle.asObservable();

    /** Snapshot of the active cycle */
    get activeCycle(): Cycle | null {
        return this._activeCycle.value;
    }

    /** Load and cache the active cycle from the API (swallows 404) */
    loadActive(): Observable<Cycle | null> {
        return this.http.get<Cycle>(`${this.apiUrl}/active`).pipe(
            tap((cycle) => this._activeCycle.next(cycle)),
            catchError(() => {
                this._activeCycle.next(null);
                return of(null);
            })
        );
    }

    /** Clear the cached active cycle */
    clearActive(): void {
        this._activeCycle.next(null);
    }

    startCycle(dto: StartCycleDto): Observable<Cycle> {
        return this.http
            .post<Cycle>(`${this.apiUrl}/start`, dto)
            .pipe(tap((cycle) => this._activeCycle.next(cycle)));
    }

    setupCycle(id: string, dto: SetupCycleDto): Observable<Cycle> {
        return this.http
            .put<Cycle>(`${this.apiUrl}/${id}/setup`, dto)
            .pipe(tap((cycle) => this._activeCycle.next(cycle)));
    }

    openCycle(id: string): Observable<Cycle> {
        return this.http
            .put<Cycle>(`${this.apiUrl}/${id}/open`, {})
            .pipe(tap((cycle) => this._activeCycle.next(cycle)));
    }

    freezeCycle(id: string): Observable<Cycle> {
        return this.http
            .put<Cycle>(`${this.apiUrl}/${id}/freeze`, {})
            .pipe(tap((cycle) => this._activeCycle.next(cycle)));
    }

    completeCycle(id: string): Observable<Cycle> {
        return this.http
            .put<Cycle>(`${this.apiUrl}/${id}/complete`, {})
            .pipe(tap((cycle) => this._activeCycle.next(cycle)));
    }

    cancelCycle(id: string): Observable<Cycle> {
        return this.http
            .delete<Cycle>(`${this.apiUrl}/${id}`)
            .pipe(tap(() => this._activeCycle.next(null)));
    }

    getHistory(): Observable<Cycle[]> {
        return this.http.get<Cycle[]>(`${this.apiUrl}/history`);
    }
}
