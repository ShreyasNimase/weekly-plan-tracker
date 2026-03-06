import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    Assignment,
    ClaimBacklogItemDto,
    UpdateAssignmentDto,
    UpdateProgressDto,
} from '../../shared/models/assignment.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AssignmentService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/assignments`;
    private readonly plansUrl = `${environment.apiUrl}/member-plans`;

    claim(dto: ClaimBacklogItemDto): Observable<Assignment> {
        return this.http.post<Assignment>(this.apiUrl, dto);
    }

    updateHours(id: string, dto: UpdateAssignmentDto): Observable<Assignment> {
        return this.http.put<Assignment>(`${this.apiUrl}/${id}`, dto);
    }

    /** PUT /api/assignments/{id}/progress */
    updateProgress(id: string, dto: UpdateProgressDto): Observable<Assignment> {
        return this.http.put<Assignment>(`${this.apiUrl}/${id}/progress`, dto);
    }

    remove(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    /** PUT /api/member-plans/{cycleMemberId}/ready */
    markReady(cycleMemberId: string): Observable<unknown> {
        return this.http.put(`${this.plansUrl}/${cycleMemberId}/ready`, {});
    }
}
