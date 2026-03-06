import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DataService {
    private readonly http = inject(HttpClient);
    private readonly base = environment.apiUrl;

    /**
     * GET /api/export
     * Returns the raw Blob for the JSON backup file.
     */
    downloadBackup(): Observable<Blob> {
        return this.http.get(`${this.base}/export`, { responseType: 'blob' });
    }

    /**
     * POST /api/import
     * Sends the parsed JSON object to the backend.
     */
    uploadBackup(data: unknown): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.base}/import`, data);
    }

    /**
     * POST /api/seed
     * Populates the database with sample data.
     */
    seedData(): Observable<{ message: string; teamMembers: number; backlogItems: number }> {
        return this.http.post<any>(`${this.base}/seed`, {});
    }

    /**
     * DELETE /api/reset
     * Wipes all data from the database.
     */
    resetApp(): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.base}/reset`);
    }
}
