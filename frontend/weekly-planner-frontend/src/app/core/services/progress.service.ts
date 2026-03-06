import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    CycleProgress,
    MemberProgress,
    CategoryProgress,
    CycleDetail,
} from '../../shared/models/progress.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ProgressService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.apiUrl}/cycles`;

    /** GET /api/cycles/{id}/progress — Full cycle progress dashboard */
    getCycleProgress(cycleId: string): Observable<CycleProgress> {
        return this.http.get<CycleProgress>(`${this.baseUrl}/${cycleId}/progress`);
    }

    /** GET /api/cycles/{id}/members/{cycleMemberId}/progress — Single member view */
    getMemberProgress(cycleId: string, cycleMemberId: string): Observable<MemberProgress> {
        return this.http.get<MemberProgress>(
            `${this.baseUrl}/${cycleId}/members/${cycleMemberId}/progress`
        );
    }

    /** GET /api/cycles/{id}/category-progress — Category utilization */
    getCategoryProgress(cycleId: string): Observable<CategoryProgress[]> {
        return this.http.get<CategoryProgress[]>(`${this.baseUrl}/${cycleId}/category-progress`);
    }

    /** GET /api/cycles/{id} — Full cycle detail (includes CycleMember.Id for each member) */
    getCycleDetail(cycleId: string): Observable<CycleDetail> {
        return this.http.get<CycleDetail>(`${this.baseUrl}/${cycleId}`);
    }
}
