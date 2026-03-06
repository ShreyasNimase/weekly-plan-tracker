export interface TeamMember {
    id: string;      // Guid
    name: string;
    isLead: boolean;
    isActive: boolean;
    createdAt: string;
}

export interface CreateTeamMemberDto {
    name: string;
    isLead: boolean;
}

export interface UpdateTeamMemberDto {
    name: string;
}
