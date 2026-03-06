import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    BacklogItem,
    CreateBacklogItemDto,
    UpdateBacklogItemDto,
    BacklogFilterParams,
} from '../../shared/models/backlog-item.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BacklogService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/backlog`;

    getAll(filters?: BacklogFilterParams): Observable<BacklogItem[]> {
        let params = new HttpParams();
        if (filters?.category) params = params.set('category', filters.category);
        if (filters?.status) params = params.set('status', filters.status);
        if (filters?.search) params = params.set('search', filters.search);
        return this.http.get<BacklogItem[]>(this.apiUrl, { params });
    }

    getById(id: string): Observable<BacklogItem> {
        return this.http.get<BacklogItem>(`${this.apiUrl}/${id}`);
    }

    create(dto: CreateBacklogItemDto): Observable<BacklogItem> {
        return this.http.post<BacklogItem>(this.apiUrl, dto);
    }

    update(id: string, dto: UpdateBacklogItemDto): Observable<BacklogItem> {
        return this.http.put<BacklogItem>(`${this.apiUrl}/${id}`, dto);
    }

    archive(id: string): Observable<BacklogItem> {
        return this.http.put<BacklogItem>(`${this.apiUrl}/${id}/archive`, {});
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
