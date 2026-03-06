import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    TeamMember,
    CreateTeamMemberDto,
    UpdateTeamMemberDto,
} from '../../shared/models/team-member.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TeamService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/team-members`;

    getAll(): Observable<TeamMember[]> {
        return this.http.get<TeamMember[]>(this.apiUrl);
    }

    getById(id: string): Observable<TeamMember> {
        return this.http.get<TeamMember>(`${this.apiUrl}/${id}`);
    }

    create(dto: CreateTeamMemberDto): Observable<TeamMember> {
        return this.http.post<TeamMember>(this.apiUrl, dto);
    }

    update(id: string, dto: UpdateTeamMemberDto): Observable<TeamMember> {
        return this.http.put<TeamMember>(`${this.apiUrl}/${id}`, dto);
    }

    makeLead(id: string): Observable<TeamMember> {
        return this.http.put<TeamMember>(`${this.apiUrl}/${id}/make-lead`, {});
    }

    deactivate(id: string): Observable<TeamMember> {
        return this.http.put<TeamMember>(`${this.apiUrl}/${id}/deactivate`, {});
    }

    reactivate(id: string): Observable<TeamMember> {
        return this.http.put<TeamMember>(`${this.apiUrl}/${id}/reactivate`, {});
    }
}
