import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardData } from '../../shared/models/dashboard.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/dashboard`;

    /** GET /api/dashboard */
    get(): Observable<DashboardData> {
        return this.http.get<DashboardData>(this.apiUrl);
    }
}
