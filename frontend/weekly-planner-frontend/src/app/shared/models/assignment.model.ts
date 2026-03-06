// Response from POST /api/assignments and GET /api/assignments/{id}
export interface Assignment {
    id: string;                 // Guid
    cycleMemberId: string;      // Guid — CycleMember.Id (not TeamMember.Id)
    backlogItemId: string;      // Guid
    backlogItemTitle: string;
    backlogItemCategory: string;
    plannedHours: number;
    createdAt: string;
    // Progress fields (available after cycle is Frozen)
    progressStatus?: string;    // NOT_STARTED | IN_PROGRESS | COMPLETED | BLOCKED
    hoursCompleted?: number;
    notes?: string;
}

// POST /api/assignments
export interface ClaimBacklogItemDto {
    cycleMemberId: string;      // Guid
    backlogItemId: string;      // Guid
    plannedHours: number;       // 0.5 increments, max 30
}

// PUT /api/assignments/{id}
export interface UpdateAssignmentDto {
    plannedHours: number;
}

// PUT /api/assignments/{id}/progress
export interface UpdateProgressDto {
    progressStatus: string;     // NOT_STARTED | IN_PROGRESS | COMPLETED | BLOCKED
    hoursCompleted: number;
    notes?: string;
}
